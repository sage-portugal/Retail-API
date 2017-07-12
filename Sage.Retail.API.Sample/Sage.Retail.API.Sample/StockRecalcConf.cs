using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.Retail.API.Sample {
    public class StockRecalcConf : RTLSystem16.IGenericSetting {
        private string _myItemId = string.Empty;

        public bool ExportToBranch {
            get {
                return false;
            }
        }

        public string Key1 {
            get {
                return "XStockRecalc";
            }
        }

        public string Key2 {
            get {
                return _myItemId;
            }
            set {
                _myItemId = value;
            }
        }

        public DateTime RecalcStartDate { get; set; }

        public void FromXML(string xml) {
            DateTime d = DateTime.Now;
            if (! DateTime.TryParse(xml, out d)) {
                d = DateTime.Now;
            }
        }

        public string ToXML() {
            return RecalcStartDate.ToString();
        }
    }
}
