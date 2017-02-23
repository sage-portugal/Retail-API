using RTLDL16;
using RTLSystem16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RTLExtenderSample {
    public static class MyApp {
        #region Global multi Uses
        private static RTLSystem16.GlobalSettings _rtlSysGlobalSettings = null;
        private static RTLData16.GlobalSettings _rtlDataGlobalSettings = null;
        private static RTLDL16.GlobalSettings _rtlDLGlobalSettings = null;
        private static RTLBL16.GlobalSettings _rtlBLGlobals = null;
        
        private static RTLSystem16.GlobalSettings rtlGlobalSettings {
            get {
                if( _rtlSysGlobalSettings == null) {
                    _rtlSysGlobalSettings = new RTLSystem16.GlobalSettings();
                }
                return _rtlSysGlobalSettings;
            }
        }

        private static RTLBL16.GlobalSettings rtlBLGlobals {
            get {
                if (_rtlBLGlobals == null) {
                    _rtlBLGlobals = new RTLBL16.GlobalSettings();
                }
                return _rtlBLGlobals;
            }
        }


        public static SystemSettings SystemSettings {
            get {
                return rtlGlobalSettings.SystemSettings;
            }
        }
        public static RTLData16.DataManager DataManager {
            get {
                if (_rtlDataGlobalSettings == null) {
                    _rtlDataGlobalSettings = new RTLData16.GlobalSettings();
                }
                return _rtlDataGlobalSettings.DataManager;
            }
        }
        public static DSOFactory DSOCache {
            get {
                if (_rtlDLGlobalSettings == null) {
                    _rtlDLGlobalSettings = new RTLDL16.GlobalSettings();
                }
                return _rtlDLGlobalSettings.DSOCache;
            }
        }
        /// <summary>
        /// Retail Federal Tax Id Validator
        /// </summary>
        public static RTLBL16.FederalTaxValidator FederalTaxValidator { get { return rtlBLGlobals.FederalTaxValidator; } }
        /// <summary>
        /// Tradutor
        /// </summary>
        public static RTLLocalize16._ILocalizer gLng { get { return rtlGlobalSettings.gLng; } }

        public static QuickSearch CreateQuickSearch(QuickSearchViews QuickSearchId, bool CacheIt) {
            return _rtlSysGlobalSettings.CreateQuickSearch(QuickSearchId, CacheIt);
        }
        #endregion
    }
}
