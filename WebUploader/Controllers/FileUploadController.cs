using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WebUploader.Models;

namespace WebUploader.Controllers
{
    public class FileUploadController : Controller
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        public FileUploadController(IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnvironment = webHostEnvironment;
        }

        /// <summary>
        /// 文件存放路径
        /// </summary>
        const string UploadPath = "\\UploadFiles\\";

        /// <summary>
        /// 测试页 - 上传文件页面
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            return View();
        }

        #region 获取指定文件的已上传的文件块
        /// <summary>
        /// 获取指定文件的已上传的文件块
        /// </summary>
        /// <returns></returns>
        public string GetMaxChunk()
        {
            int HaveFile = 0;//文件是否已存在
            string SavePath = _webHostEnvironment.ContentRootPath + UploadPath;
            try
            {
                var md5 = Convert.ToString(Request.Form["md5"]);
                var ext = Convert.ToString(Request.Form["ext"]);
                int chunk = 0;

                var fileName = md5 + "." + ext;

                FileInfo file = new FileInfo(SavePath + fileName);
                if (file.Exists)
                {
                    chunk = Int32.MaxValue;
                    HaveFile = 1;
                }
                else
                {
                    if (Directory.Exists(SavePath + "chunk\\" + md5))
                    {
                        DirectoryInfo dicInfo = new DirectoryInfo(SavePath + "chunk\\" + md5);
                        var files = dicInfo.GetFiles();
                        chunk = files.Count();
                        if (chunk > 1)
                        {
                            chunk = chunk - 1; //当文件上传中时，页面刷新，上传中断，这时最后一个保存的块的大小可能会有异常，所以这里直接删除最后一个块文件
                        }
                    }
                }
                string savePath = System.Web.HttpUtility.UrlEncode(SavePath + fileName);//保存路径
                string virtualPath = System.Web.HttpUtility.UrlEncode(UploadPath + fileName);//虚拟路径
                return CommonResult.ToJsonStr(1, string.Empty, "{\"chunk\":\"" + chunk + "\",\"savePath\":\"" + savePath + "\",\"virtualPath\":\"" + virtualPath + "\",\"HaveFile\":\"" + HaveFile + "\"}");
            }
            catch (Exception ex)
            {
                return CommonResult.ToJsonStr(0, ex.Message, "{\"chunk\":0,\"HaveFile\":\"" + HaveFile + "\"}");
            }
        }
        #endregion

        #region 上传文件
        /// <summary>
        /// 上传文件
        /// </summary>
        /// <param name="fileData"></param>
        /// <returns></returns>
        public string Upload()
        {
            var file = Request.Form.Files[0];
            string SavePath = _webHostEnvironment.ContentRootPath + UploadPath;

            string md5_key = string.Format("{0}md5", Request.Form["id"]);
            string md5_val = Request.Form[md5_key];
            //如果进行了分片
            if (Request.Form.Keys.Any(m => m == "chunk"))
            {
                //取得chunk和chunks
                int chunk = Convert.ToInt32(Request.Form["chunk"]);//当前分片在上传分片中的顺序（从0开始）
                int chunks = Convert.ToInt32(Request.Form["chunks"]);//总分片数
                //根据GUID创建用该GUID命名的临时文件夹
                string folder = SavePath + "chunk\\" + md5_val + "\\";
                string path = folder + chunk;

                //建立临时传输文件夹
                if (!Directory.Exists(Path.GetDirectoryName(folder)))
                {
                    Directory.CreateDirectory(folder);
                }

                FileStream addFile = null;
                BinaryWriter AddWriter = null;
                Stream stream = null;
                BinaryReader TempReader = null;
                try
                {
                    //addFile = new FileStream(path, FileMode.Append, FileAccess.Write);
                    addFile = new FileStream(path, FileMode.Create, FileAccess.Write);
                    AddWriter = new BinaryWriter(addFile);
                    //获得上传的分片数据流
                    stream = file.OpenReadStream();
                    TempReader = new BinaryReader(stream);
                    //将上传的分片追加到临时文件末尾
                    AddWriter.Write(TempReader.ReadBytes((int)stream.Length));
                }
                finally
                {
                    if (addFile != null)
                    {
                        addFile.Close();
                        addFile.Dispose();
                    }
                    if (AddWriter != null)
                    {
                        AddWriter.Close();
                        AddWriter.Dispose();
                    }
                    if (stream != null)
                    {
                        stream.Close();
                        stream.Dispose();
                    }
                    if (TempReader != null)
                    {
                        TempReader.Close();
                        TempReader.Dispose();
                    }
                }
                string savePath = SavePath + md5_val + Path.GetExtension(Request.Form.Files[0].FileName);
                string virtualPath = System.Web.HttpUtility.UrlEncode(UploadPath + md5_val + Path.GetExtension(Request.Form.Files[0].FileName));//虚拟路径
                return CommonResult.ToJsonStr(0, string.Empty, "{\"chunked\" : true, \"ext\" : \"" + Path.GetExtension(file.FileName) + "\",\"savePath\":\"" + System.Web.HttpUtility.UrlEncode(savePath) + "\",\"virtualPath\":\"" + virtualPath + "\"}");
            }
            else//没有分片直接保存
            {
                string path = SavePath + md5_val + Path.GetExtension(Request.Form.Files[0].FileName);
                string virtualPath = System.Web.HttpUtility.UrlEncode(UploadPath + md5_val + Path.GetExtension(Request.Form.Files[0].FileName));//虚拟路径
                //存储文件
                using (FileStream fs = System.IO.File.Create(path))
                {
                    file.CopyTo(fs);
                    fs.Flush();
                }
                return CommonResult.ToJsonStr(0, string.Empty, "{\"chunked\" : false,\"savePath\":\"" + System.Web.HttpUtility.UrlEncode(path) + "\",\"virtualPath\":\"" + virtualPath + "\"}");
            }
        }
        #endregion

        #region 合并文件
        /// <summary>
        /// 合并文件
        /// </summary>
        /// <returns></returns>
        public string MergeFiles()
        {
            string SavePath = _webHostEnvironment.ContentRootPath + UploadPath;

            string guid = Request.Form["md5"];
            string ext = Request.Form["ext"];
            string sourcePath = Path.Combine(SavePath, "chunk\\" + guid + "\\");//源数据文件夹
            string targetPath = Path.Combine(SavePath, guid + ext);//合并后的文件
            string virtualPath = System.Web.HttpUtility.UrlEncode(UploadPath + guid + ext);//虚拟路径

            DirectoryInfo dicInfo = new DirectoryInfo(sourcePath);
            if (Directory.Exists(Path.GetDirectoryName(sourcePath)))
            {
                FileInfo[] files = dicInfo.GetFiles();
                foreach (FileInfo file in files.OrderBy(f => int.Parse(f.Name)))
                {
                    FileStream addFile = new FileStream(targetPath, FileMode.Append, FileAccess.Write);
                    BinaryWriter AddWriter = new BinaryWriter(addFile);

                    //获得上传的分片数据流 
                    Stream stream = file.Open(FileMode.Open);
                    BinaryReader TempReader = new BinaryReader(stream);
                    //将上传的分片追加到临时文件末尾
                    AddWriter.Write(TempReader.ReadBytes((int)stream.Length));
                    //关闭BinaryReader文件阅读器
                    TempReader.Close();
                    stream.Close();
                    AddWriter.Close();
                    addFile.Close();

                    TempReader.Dispose();
                    stream.Dispose();
                    AddWriter.Dispose();
                    addFile.Dispose();
                }
                DeleteFolder(sourcePath);
                //context.Response.Write("{\"chunked\" : true, \"hasError\" : false, \"savePath\" :\"" + System.Web.HttpUtility.UrlEncode(targetPath) + "\"}");
                return CommonResult.ToJsonStr(0, string.Empty, "{\"chunked\" : true, \"hasError\" : false, \"savePath\" :\"" + System.Web.HttpUtility.UrlEncode(targetPath) + "\",\"virtualPath\":\"" + virtualPath + "\"}");
            }
            else
            {
                //context.Response.Write("{\"hasError\" : true}");
                return CommonResult.ToJsonStr(0, string.Empty, "{\"hasError\" : true}");
            }
        }
        #endregion

        #region 删除文件夹及其内容
        /// <summary>
        /// 删除文件夹及其内容
        /// </summary>
        /// <param name="dir"></param>
        private void DeleteFolder(string strPath)
        {
            //删除这个目录下的所有子目录
            if (Directory.GetDirectories(strPath).Length > 0)
            {
                foreach (string fl in Directory.GetDirectories(strPath))
                {
                    Directory.Delete(fl, true);
                }
            }
            //删除这个目录下的所有文件
            if (Directory.GetFiles(strPath).Length > 0)
            {
                foreach (string f in Directory.GetFiles(strPath))
                {
                    System.IO.File.Delete(f);
                }
            }
            Directory.Delete(strPath, true);
        }
        #endregion

        #region 更新用户上传记录
        /// <summary>
        /// 更新用户上传记录
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public string AddUploadRecord()
        {
            //...
            return CommonResult.ToJsonStr();
        }
        #endregion

    }
}
