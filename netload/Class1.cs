using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.AutoCAD.Runtime;

[assembly:CommandClass(typeof(netload.Class1))]

namespace netload
{
    public class Class1
    {
        [CommandMethod("netloadx")]
        public void netlaodx()
        {
            string file_dir = "";
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "dll文件(*.dll)|*.dll";

            ofd.Title = "打开dll文件";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                file_dir = ofd.FileName;
            }

            else return;
            byte[] buffer = System.IO.File.ReadAllBytes(file_dir);
            Assembly assembly = Assembly.Load(buffer);
        }



    }
}
