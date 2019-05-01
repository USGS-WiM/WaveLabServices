//Unit testing involves testing a part of an app in isolation from its infrastructure and dependencies. 
//When unit testing controller logic, only the contents of a single action is tested, not the behavior of 
//its dependencies or of the framework itself. As you unit test your controller actions, make sure you focus 
//only on its behavior. A controller unit test avoids things like filters, routing, or model binding. By focusing 
//on testing just one thing, unit tests are generally simple to write and quick to run. A well-written set of unit 
//tests can be run frequently without much overhead. However, unit tests do not detect issues in the interaction 
//between components, which is the purpose of integration testing.

using System;
using Xunit;
using System.Threading.Tasks;
using WaveLabAgent;
using WaveLabServices.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using WIM.Resources;

namespace WaveLabServices.Test
{
    public class WaveLabTest
    {
        public ProceduresController controller { get; private set; }
        public WaveLabTest() {
            //Arrange
            controller = new ProceduresController(new InMemoryWaveLabAgent());
            //must set explicitly for tests to work
            controller.ObjectValidator = new InMemoryModelValidator();
        }

        [Fact]
        public async Task Get()
        {
            //Arrange
            var id = 1;

            //Act
            var response = controller.Get();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(response);
            var result = Assert.IsType<string>(okResult.Value);
            
            Assert.Equal("MockTestRole1", result);
        }
        
    }

    public class InMemoryWaveLabAgent : IWaveLabAgent
    {

        public List<Message> Messages => throw new NotImplementedException();

        public InMemoryWaveLabAgent()
        {
           
        }
        public string method()
        {
            return "MockTestRole1";
        }
    }
    public class InMemoryModelValidator : IObjectModelValidator
    {
        public void Validate(ActionContext actionContext, ValidationStateDictionary validationState, string prefix, object model)
        {
            //assume all is valid
            return;
        }        
    }
}
