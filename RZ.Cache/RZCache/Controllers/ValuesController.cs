﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Caching.Memory;

namespace RZWCF.Controllers
{
    [Route("/")]
    public class rootController : Controller
    {
        private readonly IConfiguration _config;
        public rootController(IConfiguration config)
        {
            this._config = config;
        }

        [HttpGet]
        public string get()
        {
            string sVersion = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;

            return "RuckZuck-Proxy (c) 2018 by Roger Zander; Version: " + sVersion;
        }


    }

    [Route("/SW")]
    public class testController : Controller
    {
        private readonly IConfiguration _config;
        public testController(IConfiguration config)
        {
            this._config = config;
        }

        [HttpGet]
        public string get()
        {
            string sParent = _config.GetSection("RuckZuck:ParentServer").Value;
            return "Parent: " + sParent;
            //return "ok";
        }


    }

  
    [Route("/rest")]
    public class RZController : Controller
    {
        private readonly IConfiguration _config;
        private IMemoryCache _cache;

        public RZController(IConfiguration config, IMemoryCache memoryCache)
        {
            this._config = config;
            _cache = memoryCache;

            RuckZuck_WCF.RZRestProxy.sURL = config.GetSection("ParentServer").Value ?? _config.GetSection("RuckZuck:ParentServer").Value;
            RuckZuck_WCF.RZRestProxy.CatalogTTL =  int.Parse(_config.GetSection("CatalogTTL").Value ?? _config.GetSection("RuckZuck:CatalogTTL").Value);
            RuckZuck_WCF.RZRestProxy.localURL = config.GetSection("localURL").Value ?? _config.GetSection("RuckZuck:localURL").Value;
            RuckZuck_WCF.RZRestProxy.ipfs_GW_URL = config.GetSection("IPFS_GW_URL").Value ?? _config.GetSection("RuckZuck:ipfs_GW_URL").Value;
            RuckZuck_WCF.RZRestProxy._cache = _cache;
            RuckZuck_WCF.RZRestProxy.Proxy = config.GetSection("Proxy").Value ?? _config.GetSection("RuckZuck:Proxy").Value;
            RuckZuck_WCF.RZRestProxy.ProxyUserPW = config.GetSection("ProxyUserPW").Value ?? _config.GetSection("RuckZuck:ProxyUserPW").Value;
            if (RuckZuck_WCF.RZRestProxy.RedirectToIPFS == false)
            {
                RuckZuck_WCF.RZRestProxy.UseIPFS = int.Parse(_config.GetSection("UseIPFS").Value ?? _config.GetSection("RuckZuck:UseIPFS").Value);
            }

        }

        [Route("AuthenticateUser")]
        public ActionResult AuthenticateUser()
        {
            RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            if(RuckZuck_WCF.RZRestProxy.contentType == "*/*")
            {
                RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            }
            string Username = _config.GetSection("RuckZuck:RZUser").Value ?? _config.GetSection("RZUser").Value ?? Request.Headers["Username"];
            string Password = _config.GetSection("RuckZuck:RZPW").Value ?? _config.GetSection("RZPW").Value ?? Request.Headers["Password"];

            if (string.IsNullOrEmpty(Username))
            {
                Username = Request.Headers["Username"];
                Password = Request.Headers["Password"];
            }

            if (string.IsNullOrEmpty(Username))
            {
                return Content("", "text/xml");
            }
            else
            {
                if (Username == "FreeRZ")
                {
                    try
                    {
                        byte[] data = Convert.FromBase64String(Password);
                        DateTime when = DateTime.FromBinary(BitConverter.ToInt64(data, 0));
                        if ((DateTime.UtcNow - when) <= new TimeSpan(2, 0, 10))
                        {
                            // Create and store the AuthenticatedToken before returning it
                            string token = Guid.NewGuid().ToString();

                            return Content(token, "text/xml");
                        }
                    }
                    catch { }

                    return Content("", "text/xml");
                }
                else
                {
                    return Content(RuckZuck_WCF.RZRestProxy.GetAuthToken(Username, Password), "text/xml"); ;
                }
            }

        }

        [HttpGet]
        [Route("SWResults")]
        [Route("SWResults/{search}")]
        public ActionResult SWResults(string search = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            //RuckZuck_WCF.RZRestProxy.contentType = "application/json";

            string sRes = RuckZuck_WCF.RZRestProxy.SWResults(search);
            return Content(sRes, "text/xml");
        }

        [HttpGet]
        [Route("SWGetShort")]
        [Route("SWGetShort/{name}")]
        public ActionResult SWGet(string name = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            return Content(RuckZuck_WCF.RZRestProxy.SWGet(name), "text/xml");
        }

        [HttpGet]
        [Route("SWGet")]
        [Route("SWGet/{name}/{ver}")]
        public ActionResult SWGet(string name = "", string ver = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            return Content(RuckZuck_WCF.RZRestProxy.SWGet(name, ver), "text/xml");
        }

        [HttpGet]
        [Route("SWGet")]
        [Route("SWGet/{name}/{manuf}/{ver}")]
        public ActionResult SWGet(string name = "", string manuf = "", string ver = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            return Content(RuckZuck_WCF.RZRestProxy.SWGet(name, manuf, ver), "text/xml");
        }

        [HttpGet]
        [Route("GetSWDefinition")]
        [Route("GetSWDefinition/{name}/{ver}/{man}")]
        public ActionResult GetSWDefinition(string name = "", string ver = "", string man = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            return Content(RuckZuck_WCF.RZRestProxy.GetSWDefinitions(name, ver, man), "text/xml");
        }

        [HttpGet]
        [Route("Feedback")]
        [Route("Feedback/{name}/{ver}/{man}/{arch}/{ok}/{user}/{text}")]
        public ActionResult Feedback(string name, string ver, string man = "", string arch = "", string ok = "", string user = "", string text = "")
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            return Content(RuckZuck_WCF.RZRestProxy.Feedback(name, ver, man, arch, ok, user, text).Result, "text/xml");
        }

        [HttpGet]
        [Route("GetIcon")]
        [Route("GetIcon/{id}")]
        public Stream GetIcon(Int32 id = 575633)
        {
            return RuckZuck_WCF.RZRestProxy.GetIcon(id);
        }

        [HttpGet]
        [Route("TrackDownloadsNew")]
        [Route("TrackDownloadsNew/{SWId}/{arch}")]
        public void TrackDownloadsNew(string SWId, string arch)
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            RuckZuck_WCF.RZRestProxy.TrackDownloadsNew(SWId, arch);
        }

        [HttpPost]
        [Route("CheckForUpdateXml")]
        public ActionResult CheckForUpdateXml()
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            var oGet = new StreamReader(Request.Body).ReadToEndAsync();
            
            return Content(RuckZuck_WCF.RZRestProxy.CheckForUpdate(oGet.Result.ToString()), "text/xml");
        }

        [HttpPost]
        [Route("CheckForUpdate")]
        public ActionResult CheckForUpdate()
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            var oGet = new StreamReader(Request.Body).ReadToEndAsync();

            return Content(RuckZuck_WCF.RZRestProxy.CheckForUpdate(oGet.Result.ToString()), "text/xml");
        }

        [HttpPost]
        [Route("UploadSWEntry")]
        public bool UploadSWEntry()
        {
            //RuckZuck_WCF.RZRestProxy.contentType = (string)Request.Headers["Accept"] ?? "application/json";
            RuckZuck_WCF.RZRestProxy.contentType = "application/json";
            var oGet = new StreamReader(Request.Body).ReadToEndAsync();

            return RuckZuck_WCF.RZRestProxy.UploadSWEntry(oGet.Result.ToString());
        }

        [HttpGet]
        [Route("dl")]
        [Route("dl/{contentid}/{filename}")]
        public Stream Dl(string contentid, string filename)
        {
            return RuckZuck_WCF.RZRestProxy.GetFile(contentid + "\\" + filename);
        }
    }

    public class GetSoftware
    {
        public string ProductName { get; set; }

        public string Manufacturer { get; set; }

        public string Description { get; set; }

        public string Shortname { get; set; }

        public string ProductURL { get; set; }

        public string ProductVersion { get; set; }

        public byte[] Image { get; set; }

        public Int32? Quality { get; set; }

        public Int32? Downloads { get; set; }

        public List<string> Categories { get; set; }

        public long IconId { get; set; }
    }
}
