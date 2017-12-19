using RTLSystem16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Sage.Retail.API.Sample {
    internal static class ItemTransactionHelper {
        internal static DocumentTypeEnum TransGetType( string DocumentId ) {
            DocumentTypeEnum transType = DocumentTypeEnum.dcTypeNone;
            string transDoc = DocumentId;

            if (RTLAPIEngine.SystemSettings.WorkstationInfo.Document.IsInCollection(transDoc)) {
                var doc = RTLAPIEngine.SystemSettings.WorkstationInfo.Document[transDoc];
                transType = doc.TransDocType;
            }
            return transType;
        }

        internal static PartyTypeEnum TransGetPartyType( int cmbTransPartyTypeSelectedIndex ) {
            switch (cmbTransPartyTypeSelectedIndex) {
                case 0: return PartyTypeEnum.ptSupplier;
                case 1: return PartyTypeEnum.ptCustomer;
                case 2: return PartyTypeEnum.ptNothing;

                default: return PartyTypeEnum.ptNothing;
            }
        }
    }
}
