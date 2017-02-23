using RTLAPIPRTL16;
using RTLData16;
using RTLDL16;
using RTLPrint16;
using RTLSystem16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

public static class RTLAPIEngine {
    public class MessageEventArgs : EventArgs {
        public string Prompt { get; set; }
        public MessageBoxButtons Buttons { get; set; }
        public MessageBoxDefaultButton DefaultButton { get; set; }
        public MessageBoxIcon Icon { get; set; }
        public string Title { get; set; }
        public DialogResult Result { get; set; }
    }

    public delegate void MessageEventHandler( MessageEventArgs Args );
    public delegate void WarningErrorEventHandler(int Number, string Source, string Description);
    public delegate void WarningMessageEventHandler(string Message);

    private static RTLData16.GlobalSettings rtlDataGlobals = null;
    private static RTLCore16.GlobalSettings rtlCoreGlobals = null;
    private static RTLSystem16.GlobalSettings rtlSystemGlobals = null;
    private static RTLDL16.GlobalSettings rtlDLGlobals = null;
    private static RTLPrint16.GlobalSettings rtlPrintGlobals = null;
    private static RTLBL16.GlobalSettings rtlBLGlobals = null;
    //
    private static SystemManager rtlSystemManager = null;
    //
    private static DataManagerEventsClass dataManagerEvents = null; 

    public static event EventHandler APIStarted;
    public static event EventHandler APIStopped;
    public static event WarningErrorEventHandler WarningError;
    public static event WarningMessageEventHandler WarningMessage;
    public static event MessageEventHandler Message;


    /// <summary>
    /// Retail System Settings
    /// </summary>
    public static SystemSettings SystemSettings { get { return rtlSystemGlobals.SystemSettings; } }
    /// <summary>
    /// Retail data providers cache
    /// </summary>
    public static DSOFactory DSOCache { get { return rtlDLGlobals.DSOCache; } }
    /// <summary>
    /// Retail Data manager for low level data access. Not Recommended to use freely
    /// </summary>
    public static DataManager DataManager { get { return rtlDataGlobals.DataManager; } }
    /// <summary>
    /// Retail low level Printing manager. Usage not recomended.
    /// </summary>
    public static PrintingManager PrintingManager { get { return rtlPrintGlobals.PrintingManager; } }
    /// <summary>
    /// Retail Federal Tax Id Validator
    /// </summary>
    public static RTLBL16.FederalTaxValidator FederalTaxValidator { get { return rtlBLGlobals.FederalTaxValidator; } }
    /// <summary>
    /// Tradutor
    /// </summary>
    public static RTLLocalize16._ILocalizer gLng { get { return rtlSystemGlobals.gLng ; } }

    /// <summary>
    /// System manager
    /// </summary>
    private static SystemManager SystemManager {
        get {
            if (rtlSystemManager == null) {
                rtlSystemManager = new SystemManager();
                rtlSystemManager.Initialize();
            }
            return rtlSystemManager;
        }
    }


    private static bool apiInitialized = false;
    public static bool APIInitialized { get { return apiInitialized; } }

    // Colocar SEMPRE ao nivel do módulo/class para não ser descarregado indevidamente
#if SGCOAPI
    private static SystemStarter systemStarter = null;
#else
    private static SystemStarter systemStarter = null;
#endif

    /// <summary>basMain
    /// Inicializa a API do Retail
    /// Lança uma exceção se falhar
    /// </summary>
    /// <param name="companyId">Identificador da empresa a Abrir</param>
    public static void Initialize(string companyId, bool debugMode ) {
        apiInitialized = false;

        //
        Terminate();
        //
        // Init
        // 1. DataProvider (RTLData16)
        // 2. System (RTLSystem16)
        // 3. DataLayer (RTLData13)
        // 4. Core (RTLCore16)
        rtlDataGlobals = new RTLData16.GlobalSettings();
        rtlSystemGlobals = new RTLSystem16.GlobalSettings();
        rtlDLGlobals = new RTLDL16.GlobalSettings();
        rtlCoreGlobals = new RTLCore16.GlobalSettings();
        rtlPrintGlobals = new RTLPrint16.GlobalSettings();
        rtlBLGlobals = new RTLBL16.GlobalSettings();
        //
#if SGCOAPI
        systemStarter = new SystemStarter();
#else
        systemStarter = new SystemStarter();
#endif
        systemStarter.DebugMode = debugMode;
        if (systemStarter.Initialize(companyId) != 0) {
            string initError = systemStarter.InitializationError;
            systemStarter = null;
            throw new Exception(initError);
        }
        // Eventos de erros e avisos vindos da API
        dataManagerEvents = (RTLData16.DataManagerEventsClass)rtlDataGlobals.DataManager.Events;
        dataManagerEvents.__DataManagerEvents_Event_WarningMessage += dataManagerEvents___DataManagerEvents_Event_WarningMessage;
        dataManagerEvents.__DataManagerEvents_Event_WarningError += dataManagerEvents___DataManagerEvents_Event_WarningError;
        dataManagerEvents.__DataManagerEvents_Event_Message += DataManagerEvents___DataManagerEvents_Event_Message;

        //
        apiInitialized = true;
        //
        if (APIStarted != null)
            APIStarted( null, null );
    }

    private static void DataManagerEvents___DataManagerEvents_Event_Message(string Prompt, int Flags, string Title, ref int result) {
        if (Message != null) {
            var args = new MessageEventArgs() {
                Prompt = Prompt,
                Result = DialogResult.None,
                Title = Title,
                DefaultButton = (MessageBoxDefaultButton)(Flags & 0xF00),
                Buttons = (MessageBoxButtons)(Flags & 0xF),
                Icon = (MessageBoxIcon)(Flags & 0xF0)
            };
            Message(args);
            result = (int)args.Result;
        }
    }

    /// <summary>
    /// Trata os eventos vindos da API e dispara um novo evento para ser tratado pelo .NET
    /// </summary>
    /// <param name="Number"></param>
    /// <param name="Source"></param>
    /// <param name="Description"></param>
    static void dataManagerEvents___DataManagerEvents_Event_WarningError(int Number, string Source, string Description) {
        if (WarningError != null)
            WarningError(Number, Source, Description);
    }

    /// <summary>
    /// Trata os eventos vindos da API e dispara um novo evento para ser tratado pelo .NET
    /// </summary>
    /// <param name="MessageID"></param>
    /// <param name="MessageParams"></param>
    static void dataManagerEvents___DataManagerEvents_Event_WarningMessage(int MessageID, ref string[] MessageParams) {
        if (WarningMessage != null) {
            if( MessageID == 0 ){
                if( MessageParams.Length > 0 ){
                    WarningMessage( string.Join( Environment.NewLine, MessageParams ) );
                }
            }
            else{
                string msg = rtlSystemGlobals.gLng.GS2( MessageID, ref MessageParams );
                WarningMessage( msg );
            }
        }
    }


    public static QuickSearch CreateQuickSearch( QuickSearchViews QuickSearchId, bool CacheIt ) {
        return rtlSystemGlobals.CreateQuickSearch(QuickSearchId, CacheIt);
    }


    public static CompanyList GetCompanyList() {
        return SystemManager.Companies;
    }



    /// <summary>
    /// Termina a ligação à API e liberta todos os recursos
    /// </summary>
    public static void Terminate() {
        if (apiInitialized) {
            //1. QuickSearch
            if (rtlSystemGlobals != null) {
                rtlSystemGlobals.DisposeQuickSearch();
            }
            //3. Business globals
            if (rtlBLGlobals != null) {
                rtlBLGlobals.Dispose();
                rtlBLGlobals = null;
            }
            //4. Dispose CORE Global Settings
            if (rtlCoreGlobals != null) {
                rtlCoreGlobals.Dispose();
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(rtlCoreGlobals);
                rtlCoreGlobals = null;
            }
            //2. Dispose Printing Manager
            if (rtlPrintGlobals != null) {
                rtlPrintGlobals.Dispose();
                rtlPrintGlobals = null;
            }
            //5. DISPOSE DataLayer Global Settings
            if (rtlDLGlobals != null) {
                rtlDLGlobals.Dispose();
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(rtlDLGlobals);
                rtlDLGlobals = null;
            }
            //6. Dispose DataProvider
            if (rtlDataGlobals != null) {
                rtlDataGlobals.DataManager.CloseConnections();
                rtlDataGlobals.Dispose();
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(rtlDataGlobals);
                rtlDataGlobals = null;
            }
            //7. Dispose System
            if (rtlSystemGlobals != null) {
                rtlSystemGlobals.Dispose();
                //System.Runtime.InteropServices.Marshal.ReleaseComObject(rtlSystemGlobals);
                rtlSystemGlobals = null;
            }
            // Dispose System manager
            if( rtlSystemManager != null) {
                rtlSystemManager = null;
            }
            //
            apiInitialized = false;
            //
            // Fire event
            if (APIStopped != null)
                APIStopped(null, null);
        }
    }
}
