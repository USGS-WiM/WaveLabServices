//------------------------------------------------------------------------------
//----- ServiceAgent -------------------------------------------------------
//------------------------------------------------------------------------------

//-------1---------2---------3---------4---------5---------6---------7---------8
//       01234567890123456789012345678901234567890123456789012345678901234567890
//-------+---------+---------+---------+---------+---------+---------+---------+

// copyright:   2017 WIM - USGS

//    authors:  Jeremy K. Newson USGS Web Informatics and Mapping
//              
//  
//   purpose:   The service agent is responsible for initiating the service call, 
//              capturing the data that's returned and forwarding the data back to 
//              the requestor.
//
//discussion:   
//
// 

using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using WaveLabAgent.Resources;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;
using WIM.Utilities.ServiceAgent;
using System.IO;
using WIM.Exceptions.Services;
using System.Diagnostics;
using WIM.Resources;
using Newtonsoft.Json.Converters;

namespace WaveLabAgent
{
    public interface IWaveLabAgent:IDisposable
    {
        List<Procedure> GetAvailableProcedures();
        Procedure GetProcedure(string CodeOrID);
        ProcedureResult GetProcedureResultsFilePath(Procedure selectedprocedure,string workingdirectory);

    }

    public class WaveLabAgent : ExternalProcessServiceAgentBase, IWaveLabAgent
    {
        #region Properties
        private readonly IDictionary<Object, Object> _messages;
        private List<Procedure> availableProcedures{ get; set; }
        private List<ConfigurationOption> availableConfigurationOptions { get; set; }
        private Dictionary<string, List<int>> procedureConfigurations { get; set; }
        private Dictionary<string, string> wavelabresources { get; set; }
        #endregion
        #region Constructor
        public WaveLabAgent(IOptions<ProcedureSettings> ProcedureSettings, IHttpContextAccessor httpContextAccessor = null) :
            base(ProcedureSettings.Value.wavelab.baseurl, null)
        {
            _messages = httpContextAccessor?.HttpContext.Items;
            wavelabresources = ProcedureSettings.Value.wavelab.resources;
            //deep clone to ensure objects stay stateless
            availableProcedures = JsonConvert.DeserializeObject<List<Procedure>>(JsonConvert.SerializeObject(ProcedureSettings.Value.Procedures));
            //availableProcedures = ProcedureSettings.Value.Procedures;
            procedureConfigurations = ProcedureSettings.Value.ProcedureConfigurations;
            availableConfigurationOptions = ProcedureSettings.Value.Configurations;
            
        }
        #endregion
        #region Disposal and cleanup
        public void Dispose()
        {
            availableProcedures = null;
            availableConfigurationOptions = null;
            procedureConfigurations = null;
            wavelabresources = null;
        }
        #endregion
        #region Methods
        public List<Procedure> GetAvailableProcedures()
        {
            return availableProcedures;
        }

        public Procedure GetProcedure(string procedureIdentifier)
        {
            var procedure = availableProcedures.FirstOrDefault(n => String.Equals(n.ID.ToString(), procedureIdentifier, StringComparison.OrdinalIgnoreCase)
                            || String.Equals(n.Code.Trim(), procedureIdentifier.Trim(), StringComparison.OrdinalIgnoreCase));
          
            if (procedure == null) throw new WIM.Exceptions.Services.NotFoundRequestException($"procedure with identifier {procedureIdentifier} cannot be found");
            procedure.Configuration = getProcedureConfigurations(procedure.Code);
            return procedure;
        }

        public ProcedureResult GetProcedureResultsFilePath(Procedure selectedprocedure,string workingdirectory)
        {
            try
            {

                var results = new ProcedureResult(Path.Combine(workingdirectory, "wavelabResults"));
                if (!Directory.Exists(results.workspacePath))
                    Directory.CreateDirectory(results.workspacePath);

                if (!isValid(selectedprocedure)) throw new BadRequestException("One or more of the procedure's configuration options are invalid");
                results.Entity = execute(selectedprocedure, results.workspacePath);

                return results;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
        #region HELPER METHODS
        private List<ConfigurationOption> getProcedureConfigurations(string procedureCode)
        {
            try
            {
                if (!procedureConfigurations.ContainsKey(procedureCode)) return new List<ConfigurationOption>();
                var list = procedureConfigurations[procedureCode];
                var options = availableConfigurationOptions
                    .Join(list,
                          configOp => configOp.ID,
                          cl => cl,
                         (configOp, cl) => configOp).Select
                         (c => new ConfigurationOption()
                         {
                             ID = c.ID,
                             Name = c.Name,
                             Description = string.Format(c.Description, getConfigureationDescription(procedureCode,c.ID )),
                             Required = c.Required,
                             ValueType = c.ValueType,
                             Options = c.Options,
                             Value = String.IsNullOrEmpty(c.Value) ? null : getOptionValue(c.ValueType, c.Value)
                         });

                return options.ToList();
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private string getConfigureationDescription(string procedureCode, int id)
        {
            string name = string.Empty;
            switch (procedureCode)
            {                
                case "Read":
                    name= "barometric or water-level csv ";
                    break;
                case "Barometric":
                    name = "barometric csv ";
                    break;
                case "Wave":
                    if (id > 1) name = "water-level csv";
                    else name = "barometric netCDF ";
                    break;
                default:
                    return null;
            }
            return name;
        }
        private bool isValid(Procedure selectedprocedure)
        {
            try
            {
                return selectedprocedure.Configuration.All(o => isValid(o));
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        private bool isValid(ConfigurationOption selectedOption)
        {
            bool validity = false;
            try
            {                
                var configOpt = this.availableConfigurationOptions.Find(c => selectedOption.ID == c.ID);
                if (configOpt == null) throw new Exception("Can't find configuration option with matching id " + selectedOption.ID);

                switch (configOpt.ValueType)
                {
                    case "string":
                    case "writein-option":
                        if (selectedOption.Value is string && !string.IsNullOrEmpty(selectedOption.Value))
                            validity = true;
                        break;
                    case "option":
                        if (selectedOption.Value is string
                            && !string.IsNullOrEmpty(selectedOption.Value)&&
                            configOpt.Options.ToList().Contains(selectedOption.Value))
                            validity = true;
                            break;
                    case "coordinates array":
                        if (selectedOption.Value.GetType() == typeof(JArray))
                            selectedOption.Value = ((JArray)selectedOption.Value).ToObject<double[]>();
                        if (selectedOption.Value is Array && ((Array)selectedOption.Value).Length == 2)
                            validity = true;
                        break;
                    case "bool":
                        if (selectedOption.Value is bool && selectedOption.Value != null)
                            validity = true;
                        break;
                    case "numeric":
                        if (IsNumericType(selectedOption.Value) && selectedOption.Value != null)
                            validity = true;
                        break;
                    case "date":
                        if (selectedOption.Value is DateTime && selectedOption.Value != null)
                            validity = true;
                        break;

                    default:
                        validity = false;
                        break;
                }
                if (!validity) sm($"{selectedOption.Name} contains an invalid value. {selectedOption.Value}",MessageType.warning);
                return validity;
            }
            catch (Exception ex)
            {
                sm($"{selectedOption.Name} failed to validate", MessageType.error);
                return false;
            }
        }
        private object execute(Procedure selectedProcedure, string workingdirectory)
        {
            try
            {
                string requestargs = getRequestArguments(selectedProcedure.Code, selectedProcedure.Configuration);

                //processinfo
                ProcessStartInfo startinfo = getProcessRequest(requestargs);
                startinfo.FileName = getProcessName(selectedProcedure.Code);
                startinfo.WorkingDirectory = workingdirectory;

                var response = Execute(startinfo);
                if (!String.IsNullOrEmpty(response?.Errors)) sm("WaveLabScripts reported there was a problem " + response.Errors, MessageType.error);
                if (!string.IsNullOrEmpty(response?.Output))
                    return deserializeOutput(selectedProcedure.Code, response.Output);

                return null;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private object deserializeOutput(string procedureCode, string output)
        {
            switch (procedureCode)
            {
                case "Read":
                    var result = JsonConvert.DeserializeObject<ReadResults>(output, new Serializers.UnixDateTimeConverter());
                    return result.time.Zip(result.pressure, (s, i) => new { s, i }).ToDictionary(a => a.s,a=>a.i);
                    
                case "Barometric":
                    break;
                case "Wave":
                    break;
                default:
                    break;
            }
            throw new NotImplementedException();
        }

        private string getProcessName(string code)
        {
            if (!wavelabresources.ContainsKey(code)) throw new NotFoundRequestException(code + " process resource does not exist.");
            string resource = wavelabresources[code];
            // Path.GetFileNameWithoutExtension(resource),
            return Path.Combine(new String[] { AppContext.BaseDirectory, "Assets", "Scripts", resource });
        }
        private string getRequestArguments(string procedureCode, List<ConfigurationOption> options)
        {
            try
            {
                List<string> arguments = new List<string>();
                foreach (int identifier in procedureConfigurations[procedureCode])
                {
                    var item = options.Find(o => o.ID == identifier);
                    //input files up one directory level
                    if (item.ID == 1 || item.ID==22) item.Value = Path.Combine("..", item.Value);
                    if (item.Required && item.Value == null) throw new BadRequestException(item.Name+" is required.");
                    if (item.Value == null) item.Value = getOptionsDefaultValue(procedureCode, item.ID);
                    arguments.AddRange(getOptionValue(item));
                    options.Remove(item);
                }//next item
                if (procedureCode != "Read")
                {
                    var insertIndex = 2;
                    if (procedureCode == "Wave") insertIndex = 3;
                    arguments.InsertRange(insertIndex, new string[] { "WaveLabAgent", "wavelabagent@usgs.gov", "usgs.gov" });
                }//end if
            
            
                //wrap all in quotes
                return "\""+ string.Join("\"  \"", arguments)+ "\"";
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        private IEnumerable<string> getOptionValue(ConfigurationOption item)
        {
            var arguments= new List<string>();
            switch (item.ValueType)
            {
                case "coordinates array":
                    Array.ForEach(((double[])item.Value), x => arguments.Add(Convert.ToString(x)));
                    break;
                case "date":
                    arguments.Add(((DateTime)item.Value).ToString("yyyyMMdd HHmm"));
                    break;
                default:
                    arguments.Add(Convert.ToString(item.Value));
                    break;
            }//end switch
            return arguments;
        }
        private dynamic getOptionValue(string ValueType, string value) {
            switch (ValueType)
            {
                case "coordinates array":
                    return JsonConvert.DeserializeObject<string[]>(value);
                case "bool":
                    return Convert.ToBoolean(value);
                default:
                    return null;
            }
        }
        private dynamic getOptionsDefaultValue(string procedureCode, int optionID)
        {
            switch (optionID)
            {
                case 2:
                    switch (procedureCode)
                    {
                        default:
                            return "output";
                    }
                default:
                    return null;
            }
        }
        private bool IsNumericType(dynamic o)
        {
            switch (Type.GetTypeCode(o.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }
        private void sm(string msg, MessageType type = MessageType.info)
        {
            sm(new Message() { msg = msg, type = type });
        }
        private void sm(Message msg)
        {
            //wim_msgs comes from WIM.Standard/blob/staging/Services/Middleware/X-Messages.cs
            //manually added to avoid including libr in project.
            if (this._messages == null) return;
            if (!this._messages.ContainsKey("wim_msgs"))
                this._messages["wim_msgs"] = new List<Message>();

            ((List<Message>)this._messages["wim_msgs"]).Add(msg);
        }

        private struct ReadResults
        {
            public DateTime[] time { get; set; }
            public double[] pressure { get; set; }
           
        }
    }
    #endregion
}