using RTLBase16;
using RTLBL16;
using RTLDL16;
using RTLSystem16;
using RTLUtil16;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Sage.Retail.API.Sample {
    internal static class StockHelper {
        #region Stock Transaction

        internal static double CalculateQuantity(string strFormula, ItemTransactionDetail TransactionDetail, bool UseQuantityFactor) {
            MathFunctions mathUtil = new MathFunctions();
            UnitOfMeasure oUnit;
            BSOExpressionParser objBSOExpressionParser = new BSOExpressionParser();
            double result = 0;

            if (!string.IsNullOrEmpty(strFormula)) {
                result = 0;
                string tempres = objBSOExpressionParser.ParseFormula(strFormula, TransactionDetail);
                double.TryParse(tempres, out result);
            }
            else
                result = TransactionDetail.Quantity;

            if (UseQuantityFactor) {
                if (TransactionDetail.QuantityFactor != 1 && TransactionDetail.QuantityFactor != 0)
                    result = result / TransactionDetail.QuantityFactor;
            }

            oUnit = RTLAPIEngine.DSOCache.UnitOfMeasureProvider.GetUnitOfMeasure(TransactionDetail.UnitOfSaleID);
            if (oUnit != null) {
                result = mathUtil.MyRoundEx(result, oUnit.MaximumDecimals);
            }
            oUnit = null;

            objBSOExpressionParser = null;
            mathUtil = null;

            return result;
        }


        internal static StockQtyRuleEnum TransGetStockQtyRule(int SelectedIndex) {
            switch (SelectedIndex) {
                case 0: return StockQtyRuleEnum.stkQtyNone;
                case 1: return StockQtyRuleEnum.stkQtyReceipt;
                case 2: return StockQtyRuleEnum.stkQtyOutgoing;

                default: return StockQtyRuleEnum.stkQtyNone;
            }
        }
        #endregion
    }
}
