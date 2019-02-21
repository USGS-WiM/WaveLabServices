//------------------------------------------------------------------------------
//----- HttpController ---------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2017 WiM - USGS

//    authors:  Jeremy K. Newson USGS Web Informatics and Mapping
//              
//  
//   purpose:   Handles resources through the HTTP uniform interface.
//
//discussion:   Controllers are objects which handle all interaction with resources. 
//              
//
// 

using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;
using WaveLabAgent;
using WaveLabServices.Filters;
using WaveLabServices.Resources.Helpers;
using System.Threading.Tasks;
using System.Collections.Generic;
using WiM.Resources;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;
using System.Linq;
using System.IO;
using WaveLabAgent.Resources;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using WaveLabServices.Resources;
using System.Globalization;
using Newtonsoft.Json;
using WIM.Exceptions.Services;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Cors;

namespace WaveLabServices.Controllers
{
    [Route("[controller]")]
    public class ProceduresController : WaveLabControllerBase
    {
        public IWaveLabAgent agent { get; set; }
        private IHostingEnvironment _hostingEnvironment;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        public ProceduresController(IWaveLabAgent agent, IHostingEnvironment hostingEnvironment ) : base()
        {
            this.agent = agent;
            this._hostingEnvironment = hostingEnvironment;
        }
        #region METHODS
        [HttpGet()]
        public IActionResult Get()
        {
            //returns list of available Navigations
            try
            {
                return Ok(agent.GetAvailableProcedures());
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
        [HttpGet("{codeOrID}")]
        public IActionResult Get(string codeOrID)
        {
            //returns list of available Navigations
            try
            {
                if (string.IsNullOrEmpty(codeOrID)) return new BadRequestResult(); // This returns HTTP 404
                return Ok(agent.GetProcedure(codeOrID));
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
        [EnableCors("CorsPolicy")]
        [HttpPost]
        [DisableFormValueModelBinding]        
        [DisableRequestSizeLimit]        
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute([FromQuery]string format="")
        {
            string targetFilePath = null;
            string tempdir = Path.Combine(_hostingEnvironment.ContentRootPath, "wwwtemp");
            try
            {
                
                if (String.IsNullOrEmpty(targetFilePath))
                    targetFilePath = Path.Combine(tempdir, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
                if (!Directory.Exists(targetFilePath))
                    Directory.CreateDirectory(targetFilePath);
                

                Procedure item = await this.ProcessProcedureRequestAsync(targetFilePath);
                string resultsFilePath = agent.GetProcedureResultsFilePath(item, targetFilePath);

                if (format.Contains("zip")) return getResultZipFiles(resultsFilePath);
                else return getResultFiles(resultsFilePath);
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex);
            }
            finally
            {
                //sets path key to delete once finished
                cleanup(tempdir);
            }
        }
        #endregion
        #region HELPER METHODS
        private async Task<Procedure> ProcessProcedureRequestAsync(string targetPath)
        {
            try
            {
                Procedure result = null;
                if (!MultipartRequestHelper.IsMultipartContentType(Request.ContentType))
                    throw new BadRequestException($"Expected a multipart request, but got {Request.ContentType}");


                // Used to accumulate all the form url encoded key value pairs in the 
                // request.
                var formAccumulator = new KeyValueAccumulator();

                var boundary = MultipartRequestHelper.GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType),
                                                                   _defaultFormOptions.MultipartBoundaryLengthLimit);

                var reader = new MultipartReader(boundary, HttpContext.Request.Body);
                var section = await reader.ReadNextSectionAsync();

                while (section != null)
                {
                    ContentDispositionHeaderValue contentDisposition;
                    var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out contentDisposition);

                    if (hasContentDispositionHeader)
                    {
                        if (MultipartRequestHelper.HasFileContentDisposition(contentDisposition))
                        {
                            if (section.ContentType == "application/json")
                            {
                                var serializer = new JsonSerializer();

                                using (var sr = new StreamReader(section.Body))
                                using (var jsonTextReader = new JsonTextReader(sr))
                                    result = serializer.Deserialize<Procedure>(jsonTextReader);

                            }
                            else // is file
                            {
                                using (var targetStream = new FileStream(Path.Combine(targetPath, contentDisposition.FileName.ToString()), FileMode.Create))
                                    await section.Body.CopyToAsync(targetStream);

                            }//end if
                        }//end if
                        else if (MultipartRequestHelper.HasFormDataContentDisposition(contentDisposition))
                        {
                            // Content-Disposition: form-data; name="key"
                            // Do not limit the key name length here because the 
                            // multipart headers length limit is already in effect.
                            var key = HeaderUtilities.RemoveQuotes(contentDisposition.Name);
                            using (var streamReader = new StreamReader(section.Body, GetEncoding(section),
                                detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true))
                            {
                                // The value length limit is enforced by MultipartBodyLengthLimit
                                var value = await streamReader.ReadToEndAsync();
                                if (String.Equals(value, "undefined", StringComparison.OrdinalIgnoreCase))
                                    value = String.Empty;
                                if (isJson(value)) {
                                    result = JsonConvert.DeserializeObject<Procedure>(value);
                                }
                                else
                                {
                                    formAccumulator.Append(key.ToString(), value);

                                    if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                                        throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");
                                }
                            }//end using
                        }//endif
                    }
                    // Drains any remaining section body that has not been consumed and
                    // reads the headers for the next section.
                    section = await reader.ReadNextSectionAsync();
                }//next
                if (formAccumulator.HasValues)
                {
                    // Bind form data to a model
                    result = new Procedure();
                    var formValueProvider = new FormValueProvider(
                        BindingSource.Form,
                        new FormCollection(formAccumulator.GetResults()),
                        CultureInfo.CurrentCulture);

                    var bindingSuccessful = await TryUpdateModelAsync(result, prefix: "",
                        valueProvider: formValueProvider);
                    if (!bindingSuccessful)
                        if (!ModelState.IsValid)
                            throw new BadRequestException(ModelState.ValidationState.ToString());

                }//end if
                return result;
            }
            catch (Exception)
            {

                throw;
            }
        }
        private MultipartResult getResultFiles(string targetFilePath)
        {
            //this does not work - jkn (02/12/2019)

            //method adapted from https://github.com/aspnet/Mvc/issues/4933
            //other download methods https://www.c-sharpcorner.com/blogs/crud-operations-and-upload-download-files-in-asp-net-core-20

            try
            {
                MultipartResult result = new MultipartResult();
                foreach (string file in Directory.EnumerateFiles(targetFilePath))
                {
                    result.Add(new MultipartContent()
                    {
                        ContentType = GetContentType(file),
                        FileName = Path.GetFileName(file),
                        Stream = this.OpenFile(file)
                    });
                }//next file              

                return result;
            }
            catch (Exception)
            {
                throw;
            }
        }

        private FileResult getResultZipFiles(string resultFilePath)
        {
            try
            {
                const string contentType = "application/zip";

                var zipFile = Path.Combine(Directory.GetParent(resultFilePath).FullName, "wavelab.zip");

                ZipFile.CreateFromDirectory(resultFilePath, zipFile);
                //var stream = _hostingEnvironment.ContentRootFileProvider
                //    .GetFileInfo(zipFile).CreateReadStream();
                //return new FileStreamResult(stream, contentType);

                var result = new FileContentResult(System.IO.File.ReadAllBytes(zipFile), contentType)
                {
                    FileDownloadName = Path.GetFileName(zipFile)
                };
                return result;
            }
            catch (Exception ex)
            {

                throw;
            }            
        }
        private Stream OpenFile(string file)
        {
            return System.IO.File.Open(
                    file,
                    FileMode.Open,
                    FileAccess.Read);
        }
        private static Encoding GetEncoding(MultipartSection section)
        {
            MediaTypeHeaderValue mediaType;
            var hasMediaTypeHeader = MediaTypeHeaderValue.TryParse(section.ContentType, out mediaType);
            // UTF-7 is insecure and should not be honored. UTF-8 will succeed in 
            // most cases.
            if (!hasMediaTypeHeader || Encoding.UTF7.Equals(mediaType.Encoding))
            {
                return Encoding.UTF8;
            }
            return mediaType.Encoding;
        }
        private string GetContentType(string fileName)
        {
            //https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types/Complete_list_of_MIME_types
            var ext = Path.GetExtension(fileName);
            switch (ext.ToLower())
            {
                case ".txt": return "text/plain";
                case ".pdf": return "application/pdf";
                case ".doc": case ".docx": return "application/vnd.ms-word";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".csv": return "text/csv";
                case ".json": return "application/json";
                case ".html": return "text/html";
                case ".js": return "application/javascript";
                case ".zip": return "application/zip";
                case ".7zip": return "application/x-7z-compressed";
                default:return "application/octet-stream";
            }            
        }
        private bool isJson(string input)
        {
            input = input.Trim();
            return input.StartsWith("{") && input.EndsWith("}")
                   || input.StartsWith("[") && input.EndsWith("]");
        }
        private void cleanup(string path)
        {
            foreach (var dir in new DirectoryInfo(path).EnumerateDirectories())
                //deletes directories older than 30 min
                if (dir.LastWriteTime < DateTime.Now.AddMinutes(-30))
                     Directory.Delete(dir.FullName, true);
             
        }
        #endregion
    }
}
