using SklLib.Measurement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace vdams.Monitoring
{
    struct CameraInfo
    {
        public string Name { get; set; }
        public DateTime LastModification { get; set; }
        public InformationSize TotalSize { get; set; }
        public InformationSize TotalSizeToday { get; set; }
        public InformationSize TotalSizeYesterday { get; set; }
    }
}
