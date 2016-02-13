﻿using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using Kudu.Core.Functions;
using Kudu.Core.Tracing;
using Kudu.Services.Arm;
using System.IO;
using Kudu.Contracts.Jobs;
using Newtonsoft.Json.Linq;

namespace Kudu.Services.Functions
{
    [ArmControllerConfiguration]
    public class FunctionController : ApiController
    {
        private readonly IFunctionManager _manager;
        private readonly ITraceFactory _traceFactory;

        public FunctionController(IFunctionManager manager, ITraceFactory traceFactory)
        {
            _manager = manager;
            _traceFactory = traceFactory;
        }

        [HttpPut]
        public async Task<HttpResponseMessage> CreateOrUpdate(string name)
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step($"FunctionsController.CreateOrUpdate({name})"))
            {
                try
                {
                    var functionEnvelope = await Request.Content.ReadAsAsync<FunctionEnvelope>();
                    return Request.CreateResponse(HttpStatusCode.Created, await _manager.CreateOrUpdate(name, functionEnvelope));
                }
                catch (FileNotFoundException ex)
                {
                    tracer.TraceError(ex);
                    return Request.CreateResponse(HttpStatusCode.NotFound, ex.Message);
                }
                catch (InvalidOperationException ex)
                {
                    tracer.TraceError(ex);
                    return Request.CreateResponse(HttpStatusCode.Conflict, ex.Message);
                }
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> List()
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step("FunctionsController.list()"))
            {
                return Request.CreateResponse(HttpStatusCode.OK, await _manager.ListFunctionsConfig());
            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> Get(string name)
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step($"FunctionsController.Get({name})"))
            {
                return Request.CreateResponse(HttpStatusCode.OK, await _manager.GetFunctionConfig(name));
            }
        }

        [HttpDelete]
        public HttpResponseMessage Delete(string name)
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step($"FunctionsController.Delete({name})"))
            {
                try
                {
                    _manager.DeleteFunction(name);
                    return Request.CreateResponse(HttpStatusCode.NoContent);
                }
                catch (FileNotFoundException ex)
                {
                    tracer.TraceError(ex);
                    return Request.CreateResponse(HttpStatusCode.NotFound, ex.Message);
                }

            }
        }

        [HttpGet]
        public async Task<HttpResponseMessage> GetHostSettings()
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step("FunctionsController.GetHostSettings()"))
            {
                return Request.CreateResponse(HttpStatusCode.OK, await _manager.GetHostConfig());
            }
        }

        [HttpPut]
        public async Task<HttpResponseMessage> PutHostSettings()
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step("FunctionsController.PutHostSettings()"))
            {
                return Request.CreateResponse(HttpStatusCode.Created, await _manager.PutHostConfig(await Request.Content.ReadAsAsync<JObject>()));
            }
        }

        [HttpGet]
        public HttpResponseMessage GetFunctionsTemplates()
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step("FunctionsController.GetFunctionsTemplates()"))
            {
                return Request.CreateResponse(HttpStatusCode.OK, _manager.GetTemplates());
            }
        }

        [HttpPost]
        public async Task<HttpResponseMessage> SyncTriggers()
        {
            var tracer = _traceFactory.GetTracer();
            using (tracer.Step("FunctionController.SyncTriggers"))
            {
                try
                {
                    await _manager.SyncTriggers();

                    return Request.CreateResponse(HttpStatusCode.OK);
                }
                catch (Exception ex)
                {
                    tracer.TraceError(ex);

                    return ArmUtils.CreateErrorResponse(Request, HttpStatusCode.BadRequest, ex);
                }
            }
        }
    }
}