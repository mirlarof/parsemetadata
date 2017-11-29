using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MetaInspector
{
    public class MetaData
    {
        public string Title;
        public string Description;
        public string Image;

        public MetaData()
        {
            Title = Description = Image = string.Empty;
        }
    }
}
