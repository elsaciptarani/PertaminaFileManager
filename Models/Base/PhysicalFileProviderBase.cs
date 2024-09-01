using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;


namespace PertaminaFileManager.Models.Base
{
    public interface IPhysicalFileProviderBase : IFileProviderBase
    {
        void RootFolder(string folderName);
    }

}
