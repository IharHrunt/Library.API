using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Library.API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Library.API.Controllers
{
    //[ApiController]
    [Route("api/files")]
    public class FilesController : Controller
    {
        private ILibraryRepository _libraryRepository;
        private IConfiguration _configuration;
        string _storage;
        private string _bucketName = "ihar-images";
        private string _awsAccessKeyS3 = "AKIAZS6...";
        private string _awsSecretKeyS3 = "N/UOu8v...";
        private string _awsAccessKeySNS = "AKIAZS...";
        private string _awsSecretKeySNS = "2puKN5...";
        private string _topic = "arn:aws:sns:us-east-1:...";

        public FilesController(ILibraryRepository libraryRepository, IConfiguration configuration)
        {
            _libraryRepository = libraryRepository;
            _configuration = configuration;
            _storage = _configuration.GetValue<string>("storages:local");
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> Download(string fileName)
        {
            if (fileName == null)
            {
                return Content("fileName not present");
            }

            var path = Path.Combine(_storage, fileName);

            try
            {
                TransferUtility fileTransferUtility = new TransferUtility(new AmazonS3Client(_awsAccessKeyS3, _awsSecretKeyS3, RegionEndpoint.USEast1));
                fileTransferUtility.Download(path, _bucketName, fileName);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            try
            {
                var memory = new MemoryStream();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(path), Path.GetFileName(path));
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("random/images")]
        public async Task<IActionResult> DownloadRandom()
        {
            var files = await _libraryRepository.GetAllFilesAsync();

            var random = new Random();
            int index = random.Next(files.Count);
            var path = files[index].Path;

            try
            {
                TransferUtility fileTransferUtility = new TransferUtility(new AmazonS3Client(_awsAccessKeyS3, _awsSecretKeyS3, RegionEndpoint.USEast1));
                fileTransferUtility.Download(path, _bucketName, files[index].Name);                

                var memory = new MemoryStream();
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, GetContentType(path), Path.GetFileName(path));
            }
            catch
            {
                return NotFound();
            }
        }

        [HttpGet("subscribe/{email}")]
        public async Task<IActionResult> Subscribe(string email)
        {
            var clientSNS = new AmazonSimpleNotificationServiceClient(_awsAccessKeySNS, _awsSecretKeySNS, Amazon.RegionEndpoint.USEast1);
            var subscribeResponse = await clientSNS.SubscribeAsync(new SubscribeRequest(_topic, "email", email));
            
            if (subscribeResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Topic subscription falied");
            }

            return Ok();
        }

        [HttpGet("unsubscribe/{arn}")]
        public async Task<IActionResult> Unsubscribe(string arn)
        {
            var clientSNS = new AmazonSimpleNotificationServiceClient(_awsAccessKeySNS, _awsSecretKeySNS, Amazon.RegionEndpoint.USEast1);
            var unsubscribeResponse = await clientSNS.UnsubscribeAsync(new UnsubscribeRequest(arn));

            if (unsubscribeResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Topic unsubscription falied");
            }

            return Ok();
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {

            if (file == null || file.Length == 0)
            {
                return Content("file not selected");
            }

            var path = Path.Combine(_storage, file.FileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            if (!System.IO.File.Exists(path))
            {
                throw new Exception();
            }

            var clientS3 = new AmazonS3Client(_awsAccessKeyS3, _awsSecretKeyS3, RegionEndpoint.USEast1);
            var fileInfo = new FileInfo(path);

           var putObjectRequest = new PutObjectRequest()
            {
                InputStream = fileInfo.OpenRead(),
                BucketName = _bucketName,
                Key = file.FileName 
            };

            var response = await clientS3.PutObjectAsync(putObjectRequest);

            if (response.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("S3 uploading failed");
            }

            string subject = "New file in S3 bucket";
            string body = $"New file {file.FileName} has been uploaded to S3 bucket {_bucketName}";
            
            var clientSNS = new AmazonSimpleNotificationServiceClient(_awsAccessKeySNS, _awsSecretKeySNS, RegionEndpoint.USEast1);
            var publishResponse = await clientSNS.PublishAsync(new PublishRequest(_topic, body, subject));
            
            if (publishResponse.HttpStatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("SNS publishishing falied");
            }

            Entities.File filedb = new Entities.File
            {
                Name = file.FileName,
                Path = path,
                ContentType = GetContentType(path).ToString()
            };

            _libraryRepository.AddFileAsync(filedb);
            if (!(await _libraryRepository.SaveAsync()))
            {
                throw new Exception("Failed to save file data to db");
            }

            return Ok();
        }

        [HttpPost("{id}")]
        public async Task<IActionResult> BlockAuthorCreation(Guid id)
        {
            if (await _libraryRepository.AuthorExistsAsync(id))
            {
                return new StatusCodeResult(StatusCodes.Status409Conflict);
            }
            return NotFound();
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.ms-word"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats  officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"}
            };
        }    
    }
}

// Create a topic
//CreateTopicRequest createTopicReq = new CreateTopicRequest("New-Topic-Name");
//CreateTopicResponse createTopicRes = await client.CreateTopicAsync(createTopicReq);
////delete an SNS topic
//DeleteTopicRequest deleteTopicRequest = new DeleteTopicRequest(createTopicRes.TopicArn);
//DeleteTopicResponse deleteTopicResponse = await client.DeleteTopicAsync(deleteTopicRequest);

