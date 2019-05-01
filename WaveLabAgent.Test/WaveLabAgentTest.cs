using System;
using Xunit;
using Microsoft.Extensions.Options;
using WaveLabAgent.Resources;
using WaveLabAgent;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace WaveLabAgent.Test
{
    public class WaveLabAgentTest
    {
        //Arrange
        IOptions<ProcedureSettings> options;
        ProcedureSettings deserializedjson;
        public WaveLabAgentTest()
        {            
            deserializedjson = JsonConvert.DeserializeObject<ProcedureSettings>(File.ReadAllText("./ProcedureSettings.json"));
            options = Options.Create<ProcedureSettings>(deserializedjson);
        }

        [Fact]
        public void GetAvailableProcedures()
        {
            //Arrange
            var mock = new WaveLabAgent(options);
            //Act
            var result = mock.GetAvailableProcedures();
            //Assert
            Assert.Equal(deserializedjson.Procedures.Count, result.Count);
        }

        [Fact]
        public void GetSpecificProcedures()
        {
            //Arrange
            var item = deserializedjson.Procedures.FirstOrDefault(p => p.ID == 1);
            var mock = new WaveLabAgent(options);

            //Act
            var result = mock.GetProcedure(item.Code);

            //Assert
            Assert.Equal(result.Name, item.Name);
        }
        [Fact]
        public void ExecuteReadProcedure()
        {
            //Arrange
            var item = deserializedjson.Procedures.FirstOrDefault(p => p.ID == 1);
            var mock = new WaveLabAgent(options);
            var procedure = mock.GetProcedure(item.Code);
            PopulateProcedure(ref procedure);

            //Act
            mock.GetProcedureResultsFilePath(procedure, @"D:\WiM\GitHub\WaveLabServices\WaveLabAgent.Test\temp");
            //Assert
            Assert.Equal(procedure.Name, procedure.Name);
        }
        [Fact]
        public void ExecuteBarometricProcedure()
        {
            //Arrange
            var item = deserializedjson.Procedures.FirstOrDefault(p => p.ID == 2);
            var mock = new WaveLabAgent(options);
            var procedure = mock.GetProcedure(item.Code);
            PopulateProcedure(ref procedure);

            //Act
            mock.GetProcedureResultsFilePath(procedure, @"D:\WiM\GitHub\WaveLabServices\WaveLabAgent.Test\temp");
            //Assert
            Assert.Equal(procedure.Name, procedure.Name);
        }
        [Fact]
        public void ExecuteWaveProcedure()
        {
            //Arrange
            var item = deserializedjson.Procedures.FirstOrDefault(p => p.ID == 4);
            var mock = new WaveLabAgent(options);
            var procedure = mock.GetProcedure(item.Code);
            PopulateProcedure(ref procedure);
            //Act
            mock.GetProcedureResultsFilePath(procedure, @"D:\WiM\GitHub\WaveLabServices\WaveLabAgent.Test\temp");
            //Assert
            Assert.Equal(procedure.Name, procedure.Name);
        }

        private void PopulateProcedure(ref Procedure procedure)
        {
            for (int i = 0; i < procedure.Configuration.Count; i++)
            {
                var option = procedure.Configuration[i];
                
                switch (option.ID)
                {
                    case 1://inputfile
                        switch (procedure.Code)
                        {
                            case "Read":
                                option.Value= "NCCAR00007_1511451_sea.csv";
                                break;
                            case "Barometric":
                                option.Value = "NCCAR12248_9983816_air.csv";
                                break;
                            case "Wave":
                                if (i > 0) option.Value = "NCCAR12248_9983816_air.nc";
                                else option.Value = "NCCAR00007_1511451_sea.csv";
                                break;
                            default:
                                option.Value = "";
                                break;
                        }
                        break;
                    case 2://outputfile
                        option.Value = "output";
                        break;
                    case 3://instrument type
                        switch (procedure.Code)
                        {
                            case "Read":
                            case "Wave":
                                option.Value = Convert.ToString(option.Options[4]);
                                break;
                            case "Barometric":
                                option.Value = Convert.ToString(option.Options[5]);
                                break;
                            default:
                                option.Value = "";
                                break;
                        }
                        break;
                    case 4://site identifier
                        option.Value = "test_site id";
                        break;
                    case 5://instrument identifier
                        option.Value = "test_instrument id";
                        break;
                    case 6://location
                        option.Value = new double[] { 39.79728106, -84.088548 };
                        break;
                    case 7://time zone
                        option.Value = Convert.ToString(option.Options[2]);
                        break;
                    case 8://daylight savings?
                        option.Value = false;
                        break;
                    case 9://Deployment distance
                        option.Value = 1;
                        break;
                    case 10://retrieval distance
                        option.Value = 1;
                        break;
                    case 11://datum
                        option.Value = Convert.ToString(option.Options[0]);
                        break;
                    case 12://start date
                        option.Value = new DateTime(2016, 10, 8, 8, 0, 0);
                        break;
                    case 13://end date
                        option.Value = new DateTime(2016, 10, 10, 8, 0, 0);
                        break;
                    case 14://Salinity
                        option.Value= Convert.ToString(option.Options[0]);
                        break;
                    case 15:// Preassure Type
                        option.Value = Convert.ToString(option.Options[0]);
                        break;
                    case 16://Deployment Datum tapedown distance
                        option.Value = 1;
                        break;
                    case 17: //Retrieval Datum Tapedown distance
                        option.Value = 1.2;
                        break;
                    case 18:// Date Deployed
                        option.Value = new DateTime(2016, 10, 02, 0, 0, 0);
                        break;
                    case 19://Date Received
                        option.Value = new DateTime(2016, 10, 10, 0, 0, 0);
                        break;
                    case 20://Waterbody
                        option.Value = Convert.ToString(option.Options[0]);
                        break;
                    case 21://sensor at 4 hz
                        option.Value = true;
                        break;
                    default:
                        option.Value = null;
                        break;
                }//end switch
            }//next
        }
    }
}
