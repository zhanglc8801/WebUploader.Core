using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using WebUploader.Models;

namespace WebUploader.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 文件存放路径
        /// </summary>
        const string UploadFiles = "/Upload";

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Image()
        {
            return View();
        }

        public IActionResult demo1()
        {
            return View();
        }

        public IActionResult demo2()
        {
            return View();
        }

        public IActionResult demo3()
        {
            return View();
        }

        public IActionResult demo4()
        {
            return View();
        }

    }
}
