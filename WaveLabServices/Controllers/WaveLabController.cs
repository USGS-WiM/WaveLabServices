﻿//------------------------------------------------------------------------------
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

namespace WaveLabServices.Controllers
{
    [Route("[controller]")]
    public class WaveLabController : WiM.Services.Controllers.ControllerBase
    {
        public IWaveLabAgent agent { get; set; }
        private IHostingEnvironment _hostingEnvironment;
        private static readonly FormOptions _defaultFormOptions = new FormOptions();
        public WaveLabController(IWaveLabAgent agent, IHostingEnvironment hostingEnvironment ) : base()
        {
            this.agent = agent;
            this._hostingEnvironment = hostingEnvironment;
        }
        #region METHODS
        [HttpGet()]
        public async Task<IActionResult> Get()
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
        public async Task<IActionResult> Get(string codeOrID)
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
        [HttpPost]
        [DisableFormValueModelBinding]
        [DisableRequestSizeLimit]
        //[ValidateAntiForgeryToken]
        public async Task<IActionResult> Execute()
        {
            string targetFilePath = null;
            try
            {
                if (String.IsNullOrEmpty(targetFilePath))
                    targetFilePath = Path.Combine(_hostingEnvironment.ContentRootPath, "wwwtemp", Path.GetFileNameWithoutExtension(Path.GetRandomFileName()));
                if (!Directory.Exists(targetFilePath))
                    Directory.CreateDirectory(targetFilePath);

                Procedure item = await this.ProcessProcedureRequestAsync(targetFilePath);
                agent.LoadProcedureFiles(item, targetFilePath);

                return Ok();
            }
            catch (Exception ex)
            {
                return await HandleExceptionAsync(ex);
            }
            finally
            {
                if (Directory.Exists(targetFilePath))
                {
                    Directory.Delete(targetFilePath, true);
                }
            }
        }
        #endregion
        #region HELPER METHODS
        private void sm(List<Message> messages)
        {
            if (messages.Count < 1) return;
            HttpContext.Items[WiM.Services.Middleware.X_MessagesExtensions.msgKey] = messages;
        }
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

                                formAccumulator.Append(key.ToString(), value);

                                if (formAccumulator.ValueCount > _defaultFormOptions.ValueCountLimit)
                                    throw new InvalidDataException($"Form key count limit {_defaultFormOptions.ValueCountLimit} exceeded.");

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
        #endregion
    }
}
