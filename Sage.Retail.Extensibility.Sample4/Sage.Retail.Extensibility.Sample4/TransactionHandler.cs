using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RTLBL16;
using RTLSystem16;
using RTLBase16;
using System.Windows.Forms;

namespace RTLExtenderSample
{
    class TransactionHandler : IDisposable
    {
        private ExtenderEvents headerEvents = null;
        private ExtenderEvents detailEvents = null;

        private BSOItemTransaction bsoItemTrans = null;
        private PropertyChangeNotifier propChangeNotifier = null;
        private double lastCustomerId = 0;

        /// <summary>
        /// Eventos disparados pelo Retail:
        /// OnInitialize:   Uma vez no arranque da aplicação
        /// OnNew:          Sempre que se inicializa uma nova linha
        /// OnValidating:   Ao validar uima linha. Pode ser cancelada a introdução da linha
        /// 
        /// Restantes eventos não são disparados.
        /// </summary>
        /// <param name="e"></param>
        public void SetDetailEventsHandler(ExtenderEvents e)
        {
            detailEvents = e;

            detailEvents.OnInitialize += DetailEvents_OnInitialize;
            detailEvents.OnValidating += DetailEvents_OnValidating;
            detailEvents.OnNew += DetailEvents_OnNew;
        }

        private void DetailEvents_OnDispose()
        {
            System.Windows.Forms.MessageBox.Show("DetailEvents_OnDispose");
        }

        private void DetailEvents_OnInitialize(object Sender, ExtenderEventArgs e)
        {
            //System.Windows.Forms.MessageBox.Show("DetailEvents_OnInitialize");
        }

        private void DetailEvents_OnNew(object Sender, ExtenderEventArgs e)
        {
            var detail = (ItemTransactionDetail)e.get_data();

            //detail.ItemID = "7up4";

            //e.result.ResultMessage = "Alterei a descrição de um artigo novo";
            //e.result.Success = true;
        }

        public void SetHeaderEventsHandler(ExtenderEvents e)
        {
            headerEvents = e;

            headerEvents.OnInitialize += HeaderEvents_OnInitialize;
            headerEvents.OnMenuItem += HeaderEvents_OnMenuItem;
            headerEvents.OnValidating += HeaderEvents_OnValidating;
            headerEvents.OnSave += HeaderEvents_OnSave;
            headerEvents.OnDelete += HeaderEvents_OnDelete;
            headerEvents.OnNew += HeaderEvents_OnNew;
            headerEvents.OnLoad += HeaderEvents_OnLoad;
            headerEvents.OnDispose += HeaderEvents_OnDispose;
        }

        private void HeaderEvents_OnDispose()
        {
            // Dispose your objects
        }

        private void HeaderEvents_OnLoad(object Sender, ExtenderEventArgs e)
        {
            var trans = (ItemTransaction)e.get_data();

            ///... Code here
            lastCustomerId = 0;
            e.result.Success = true;
        }

        private ItemTransaction _transaction = null;
        private void HeaderEvents_OnNew(object Sender, ExtenderEventArgs e)
        {
            _transaction = (ItemTransaction)e.get_data();

            //Se cliente da última venda diferente de 0
            if (lastCustomerId != 0)
            {
                double customerPoints = GetCustomerPoints(lastCustomerId); //verifica os pontos que o cliente tem

                if (customerPoints >= 200) //se cliente com mais de 200 pontos
                {
                    //retira 200 pontos ao cliente
                    Discount200Points(lastCustomerId);

                    //criar um TDE no valor de 10€ para o cliente, referência POINTS (se não existir, é criada automaticamente)
                    var transactionTDE = CreateNewDocumentTDE(bsoItemTrans.Transaction.TransSerial, bsoItemTrans.Transaction.DefaultWarehouse, "TDE", 10, lastCustomerId, bsoItemTrans.Transaction.Salesman.SalesmanID, true);

                    //Imprime o TDE (tem de estar configurado na Conf. postos para impressão)
                    BSOItemTransaction bsoItemTransaction = null;
                    bsoItemTransaction = new BSOItemTransaction();
                    bsoItemTransaction.UserPermissions = MyApp.SystemSettings.User;
                    bsoItemTransaction.PrintTransaction(transactionTDE.TransSerial, transactionTDE.TransDocument, transactionTDE.TransDocNumber, RTLPrint16.PrintJobEnum.jobPrint, 1);
                    bsoItemTransaction = null;
                }
            }
            lastCustomerId = 0;
            
        }


        private double GetCustomerPoints(double customerId)
        {
            double totalPoints = 0;

            var myCustomer = MyApp.DSOCache.CustomerProvider.GetCustomer(customerId);
            if (myCustomer != null)
            {
                totalPoints = myCustomer.FrequentShopperPoints;
                myCustomer = null;
            }
            return totalPoints;
        }

        private void Discount200Points(double customerId)
        {
            var bsoDiscPointsTransaction = new BSODiscPointsTransaction();
            bsoDiscPointsTransaction.InitNewTransaction(MyApp.SystemSettings.WorkstationInfo.DefaultTransSerial);
            bsoDiscPointsTransaction.PartyID = customerId;

            var createDate = DateTime.Today;
            bsoDiscPointsTransaction.createDate = createDate;
            bsoDiscPointsTransaction.SalesmanID = 1;
            bsoDiscPointsTransaction.DiscountedPoints(200);
            bsoDiscPointsTransaction.BaseCurrency = "EUR";
            bsoDiscPointsTransaction.SaveDocument();
        }


        private void HeaderEvents_OnDelete(object Sender, ExtenderEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("Acabei de anular.");
        }

        private void HeaderEvents_OnSave(object Sender, ExtenderEventArgs e)
        {
            var propList = (ExtendedPropertyList)e.get_data();
            var transaction = (ItemTransaction)propList.get_Value("Data");


            //Se documento ORC, atualiza os preços de venda com os preços definidos
            if (transaction.TransDocument == "ORC")
            {
                foreach (ItemTransactionDetail detail in transaction.Details)
                {
                    var myItem = MyApp.DSOCache.ItemProvider.GetItem(detail.ItemID, MyApp.SystemSettings.BaseCurrency, false);
                    if (myItem != null)
                    {
                        //myItem.Description = txtItemDescription.Text;
                        //myItem.ShortDescription = txtItemShortDescription.Text;
                        //myItem.Comments = txtItemComments.Text;
                        //
                        // Preços - PVP1
                        Price myPrice = myItem.SalePrice[1, 0];
                        // Definir o preço (neste caso, com imposto (IVA) incluido)
                        double taxIncludedPrice = Math.Round(detail.TotalTaxIncludedAmount / detail.Quantity, 2);
                        myPrice.TaxIncludedPrice = taxIncludedPrice;
                        // Obter preço unitário sem impostos
                        myPrice.UnitPrice = MyApp.DSOCache.TaxesProvider.GetItemNetPrice(
                                                            taxIncludedPrice,
                                                            myItem.TaxableGroupID,
                                                            MyApp.SystemSettings.SystemInfo.DefaultCountryID,
                                                            MyApp.SystemSettings.SystemInfo.TaxRegionID);
                        //
                        // Guardar as alterações
                        MyApp.DSOCache.ItemProvider.Save(myItem, myItem.ItemID, false);
                    }
                }
            }

            lastCustomerId = transaction.PartyID;
        }

        private void HeaderEvents_OnValidating(object Sender, ExtenderEventArgs e)
        {
            var propList = (ExtendedPropertyList)e.get_data();
            var forDeletion = (bool)propList.get_Value("forDeletion");
            var transaction = (ItemTransaction)propList.get_Value("Data");

            if (!forDeletion)//Se diferente de anulação
            {

                //Se cliente associado ao grupo 2, força a gravação de FR
                //if (transaction.PartyID != 0)
                //{
                //    var customer = MyApp.DSOCache.CustomerProvider.GetCustomer(transaction.PartyID);

                //    if (customer.CustomerLevel == 2 && transaction.TransDocument != "FR")
                //    {
                //        transaction.TransDocument = "FR";
                //        var x = MyApp.DSOCache.DocumentProvider.GetLastDocNumber(transaction.TransDocType, transaction.TransSerial, transaction.TransDocument);
                //        transaction.TransDocNumber = x + 1;
                //    }
                //    customer = null;
                //}


                foreach (ItemTransactionDetail detail in transaction.Details)
                {
                    if (detail.FamilyID == 1)
                    {
                        e.result.ResultMessage = string.Format("Não pode vender artigos da familia {0}", detail.FamilyName);
                        e.result.Success = false;
                        break;
                    }
                }


                if (e.result.Success)
                {
                    lastCustomerId = transaction.PartyID;
                }
                else
                {
                    lastCustomerId = 0;
                }

                //if (transaction.Details.Count > 3) {
                //    e.result.Success = true;
                //}
                //else {
                //    e.result.Success = false;
                //    e.result.ResultMessage = "Não é possivel gravar documentos com menos de 3 linhas";
                //}
            }
            else
            {
                e.result.ResultMessage = "Não pode anular documentos!";
                e.result.Success = true;
            }


        }


        void OnPropertyChanged(string PropertyID, ref object value, ref bool Cancel)
        {
            // HANDLE BSOItemTransaction PROPERTY CHANGES HERE

            Console.WriteLine("OnPropertyChanged {0}={1}; Cancel={2}", PropertyID, value, Cancel);
            //Cancel = false;
        }


        #region HEADER EVENTS


        /// <summary>
        /// Inicialização
        /// Podemos adicionar novas opções de menu aqui
        /// IN:
        ///     e.get_data(): ExtendedPropertyList
        ///     "PropertyChangeNotifier" = Evento que podemos subscrever para controlar quando uma propriedade é alterada
        ///     "TransactionManager" = BSOItemTransaction; Controlador da transação em curso
        /// 
        /// OUT:
        ///     result.Sucess: true para sinalizar sucesso e carregar novos menus; false para cancelar
        ///     result.ResultMessage: Ignorado
        ///     result.set_data( ExtenderMenuItems ): Items de menu a carregar 
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        void HeaderEvents_OnInitialize(object Sender, ExtenderEventArgs e)
        {
            var propList = (ExtendedPropertyList)e.get_data();
            propChangeNotifier = (PropertyChangeNotifier)propList.get_Value("PropertyChangeNotifier");
            propChangeNotifier.PropertyChanged += OnPropertyChanged;

            bsoItemTrans = (BSOItemTransaction)propList.get_Value("TransactionManager");
            bsoItemTrans.WarningItemStock += BsoItemTrans_WarningItemStock;

            var newMenus = new ExtenderMenuItems();
            //
            //Criar o grupo: Tab
            var mnuGroup = newMenus.Add("mniXCustomTools", "Custom Tools");
            //criar item1
            var mnuItem1 = mnuGroup.ChildItems.Add("mniXTrans1", "Custom Item 1");
            mnuItem1.GroupType = ExtenderGroupType.ExtenderGroupTypeExtraOptions;
            //mnuItem1.Picture = ImageConverter.GetIPictureDispFromImage(  )

            //criar item2
            mnuItem1 = mnuGroup.ChildItems.Add("mniXTrans2", "Custom Item 2");
            mnuItem1.GroupType = ExtenderGroupType.ExtenderGroupTypeExtraOptions;

            object returnMenu = newMenus;
            e.result.set_data(returnMenu);

        }

        void HeaderEvents_OnMenuItem(object Sender, ExtenderEventArgs e)
        {
            var menuId = (string)e.get_data();
            var rnd = new Random();

            switch (menuId)
            {
                case "mniXTrans1":
                    //System.Windows.Forms.MessageBox.Show("YAY");
                    double qty = rnd.Next(1, 10) + (double)rnd.Next(0, 99) / 100;
                    double unitPrice = rnd.Next(1, 100) + (double)rnd.Next(0, 99) / 100;

                    var item = MyApp.DSOCache.ItemProvider.GetItem("aaa", MyApp.SystemSettings.BaseCurrency);
                    if (item != null)
                    {
                        var detail = new ItemTransactionDetail()
                        {
                            LineItemID = bsoItemTrans.Transaction.Details.Count + 1,
                            ItemID = item.ItemID,
                            Description = item.Description
                        };
                        if (bsoItemTrans.Transaction.TransactionTaxIncluded)
                        {
                            detail.TaxIncludedPrice = unitPrice;
                        }
                        else
                        {
                            detail.UnitPrice = unitPrice;
                        }
                        detail.SetUnitOfSaleID(item.UnitOfSaleID);
                        detail.SetQuantity(qty);
                        detail.TaxableGroupID = item.TaxableGroupID;

                        //var detail = TransAddDetail(bsoItemTrans.Transaction, item, qty, "UNI", unitPrice, 23, 1, 0, 0, string.Empty, string.Empty, string.Empty, string.Empty, DateTime.Now, 0, 0, 0);
                        // IMPORTANTE: Mandar calcular a linha!
                        bsoItemTrans.BSOItemTransactionDetail.Calculate(detail);
                        // Adicionar à venda
                        bsoItemTrans.AddDetail(detail);
                    }

                    break;
            }
        }

        //switch (e.get_data().ToString().ToLower()) {
        //    case "lepeso":
        //        if (bsoItemTrans.BSOItemTransactionDetail != null) {
        //            bsoItemTrans.BSOItemTransactionDetail.HandleItemDetail("9.99", TransDocFieldIDEnum.fldQuantity);

        //            e.result.Success = true;
        //            e.result.ResultMessage = string.Empty;
        //        }
        //        else {
        //            e.result.Success = false;
        //            e.result.ResultMessage = "Não foi posivel obter o controlador da linha (BSOItemTransactionDetail)";
        //        }
        //        break;
        //}
        //}

        #endregion


        /// <summary>
        /// EXEMPLO DE VALIDAÇÃO NA LINHA   
        /// </summary>
        /// <param name="Sender"></param>
        /// <param name="e"></param>
        void DetailEvents_OnValidating(object Sender, ExtenderEventArgs e)
        {
            ExtendedPropertyList properties = (ExtendedPropertyList)e.get_data();
            ItemTransactionDetail itemTransactionDetail = (ItemTransactionDetail)properties.get_Value("Data");
            string errorMessage = string.Empty;

            var mainProvider = MyApp.DataManager.MainProvider;
            double priceWithDiscount = 0;

            try
            {
                string query = string.Format("Select PriceWithDiscount from UXDiscounColorAndSize Where ItemID='{0}' and SizeId={1} and ColorID={2}",
                                            itemTransactionDetail.ItemID, itemTransactionDetail.Size.SizeID, itemTransactionDetail.Color.ColorID);
                priceWithDiscount = Convert.ToDouble(mainProvider.ExecuteScalar(query));
            }
            catch (Exception ex)
            {
                string tableToCreateInSQL = string.Format("CREATE TABLE [dbo].[UXDiscounColorAndSize]([ItemID] [nvarchar](25) NOT NULL, [SizeID] [int] NOT NULL, [ColorID] [int] NOT NULL, [PriceWithDiscount] [float] NOT NULL ) ON [PRIMARY]", Environment.NewLine);
                MessageBox.Show(String.Format("{0}{1}{1}{1}Crie uma tabela no SQL com a seguinte informação:{1}{1}{2}", ex.Message.ToString(), Environment.NewLine, tableToCreateInSQL), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                priceWithDiscount = 0;
            }

            if (priceWithDiscount != 0)
            {
                itemTransactionDetail.TaxIncludedPrice = priceWithDiscount;
                bsoItemTrans.BSOItemTransactionDetail.Calculate(itemTransactionDetail);
            }


            //TODO:
            //Insert line handling code HERE
            //------------------------------
            //if (itemTransactionDetail.UnitOfSaleID.Equals("kg", StringComparison.CurrentCultureIgnoreCase)) {
            //    itemTransactionDetail.Quantity = 9.99;
            //}
            //------------------------------

            //if (itemTransactionDetail.Quantity > 100) {
            //    e.result.Success = false;
            //    e.result.R    esultMessage = string.Format("Atenção! Quantidade {0} superior ao permitido", itemTransactionDetail.Quantity);
            //}
            //else {
            //    e.result.Success = true;
            //    e.result.ResultMessage = string.Empty;
            //}

            // When forcing a taxable group,
            // it is necessary to recalculate all the detail
            //itemTransactionDetail.TaxableGroupID = 3;
            //bsoItemTrans.BSOItemTransactionDetail.Calculate(itemTransactionDetail);

            properties = null;
            itemTransactionDetail = null;
        }


        /// <summary>
        /// Adiciona um detalhe (linha) à transação
        /// </summary>
        /// <param name="trans"></param>
        /// <param name="itemId"></param>
        /// <param name="qty"></param>
        /// <param name="unitOfMeasureId"></param>
        /// <param name="unitPrice"></param>
        /// <param name="taxPercent"></param>
        /// <param name="whareHouseId"></param>
        private ItemTransactionDetail TransAddDetail(ItemTransaction trans, Item item, double qty, string unitOfMeasureId, double unitPrice, double taxPercent, short whareHouseId,
                                     short colorId, short sizeId,
                                     string serialNumberPropId, string serialNumberPropValue,
                                     string lotId, string lotDescription, DateTime lotExpDate, short lotReturnWeek, short lotReturnYear, short lotEditionId)
        {

            var doc = MyApp.SystemSettings.WorkstationInfo.Document[trans.TransDocument];

            ItemTransactionDetail transDetail = new ItemTransactionDetail();
            transDetail.BaseCurrency = MyApp.SystemSettings.BaseCurrency;
            transDetail.ItemID = item.ItemID;
            transDetail.CreateDate = trans.CreateDate;
            transDetail.CreateTime = trans.CreateTime;
            transDetail.ActualDeliveryDate = trans.CreateDate;
            //Utilizar a descrição do artigo, ou uma descrição personalizada
            transDetail.Description = item.Description;
            // definir a quantidade
            transDetail.Quantity = qty;
            // Preço unitário. NOTA: Ver a diferença se o documento for com impostos incluidos!
            if (trans.TransactionTaxIncluded)
                transDetail.TaxIncludedPrice = unitPrice;
            else
                transDetail.UnitPrice = unitPrice;
            // Definir a lista de unidades
            transDetail.UnitList = item.UnitList;
            // Definir a unidade de venda/compra
            transDetail.SetUnitOfSaleID(unitOfMeasureId);
            //Definir os impostos
            short TaxGroupId = MyApp.DSOCache.TaxesProvider.GetTaxableGroupIDFromTaxRate(taxPercent, MyApp.SystemSettings.SystemInfo.DefaultCountryID, MyApp.SystemSettings.SystemInfo.TaxRegionID);
            transDetail.TaxableGroupID = TaxGroupId;
            //*** Uncomment for discout
            //transDetail.DiscountPercent = 10
            //
            // Se o Armazém não existir, utilizar o default que se encontra no documento.
            if (MyApp.DSOCache.WarehouseProvider.WarehouseExists(whareHouseId))
                transDetail.WarehouseID = whareHouseId;
            else
                transDetail.WarehouseID = doc.Defaults.Warehouse;
            // Identificador da linha
            transDetail.LineItemID = trans.Details.Count + 1;
            //
            //*** Uncomment to provide line totals
            //.TotalGrossAmount =        'Line Gross amount
            //.TotalNetAmount =          'Net Gross amount
            //
            //Definir o último preço de compra
            if (doc.TransDocType == DocumentTypeEnum.dcTypePurchase)
            {
                transDetail.ItemExtraInfo.ItemLastCostTaxIncludedPrice = item.SalePrice[0].TaxIncludedPrice;
                transDetail.ItemExtraInfo.ItemLastCostUnitPrice = item.SalePrice[0].UnitPrice;
            }

            item = null;

            return transDetail;
        }

        private void BsoItemTrans_WarningItemStock(TransactionWarningsEnum MsgID, ItemTransactionDetail objItemTransactionDetail)
        {

            var wareHouses = MyApp.DSOCache.WarehouseProvider.GetWarehouseRS();
            List<StockByWareHouse> stockByWareHouseList = new List<StockByWareHouse>();

            while (!wareHouses.EOF)
            {
                short wareHouseID = Convert.ToInt16(wareHouses.Fields["WareHouseID"].Value);
                if (objItemTransactionDetail.WarehouseID != wareHouseID)
                {
                    var itemStk = MyApp.DSOCache.ItemProvider.GetItemStockOnWarehouse(objItemTransactionDetail.ItemID, wareHouseID, 0, 0);
                    StockByWareHouse stockByWareHouse = new StockByWareHouse();
                    stockByWareHouse.WareHouseID = wareHouseID;
                    stockByWareHouse.Stock = itemStk.PhysicalQty;
                    stockByWareHouseList.Add(stockByWareHouse);
                }
                wareHouses.MoveNext();
            }

            //string message = string.Format("Apenas existem {0} quantidades no armazém {1}.", actualPhysicalQty.PhysicalQty, itemTransactionDetail.WarehouseID) ;
            string message = "";
            if (stockByWareHouseList.Count() > 0)
            {
                message = message + string.Format("\nStock restantes armazéns:\n\n");
                foreach (StockByWareHouse sbw in stockByWareHouseList)
                {
                    message = message + string.Format("- Arm. {0}\t\t\t\tQnt. {1}\n", sbw.WareHouseID, sbw.Stock);
                }
            }

            MessageBox.Show(message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);

            //double dblStockQuantity = 0;
            //double dblReorderPointQuantity = 0;
            //string strMessage = string.Empty;

            //switch (MsgID)
            //{
            //    case TransactionWarningsEnum.tweItemColorSizeStockNotHavePhysical:
            //    case TransactionWarningsEnum.tweItemStockNotHavePhysical:
            //        if (objItemTransactionDetail.PackQuantity == 0)
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyPhysicalBalanceCount;
            //        }
            //        else
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyPhysicalBalanceCount / objItemTransactionDetail.PackQuantity;
            //        }
            //        strMessage = MyApp.gLng.GS((int)MsgID, new object[]{
            //                                                 objItemTransactionDetail.WarehouseID.ToString().Trim(),
            //                                                 dblStockQuantity,
            //                                                 objItemTransactionDetail.UnitOfSaleID,
            //                                                 objItemTransactionDetail.ItemID,
            //                                                 objItemTransactionDetail.Size.Description,
            //                                                 objItemTransactionDetail.Color.Description
            //                                    });

            //        break;

            //    case TransactionWarningsEnum.tweItemReorderPoint:
            //    case TransactionWarningsEnum.tweItemColorSizeReorderPoint:
            //        if (objItemTransactionDetail.PackQuantity == 0)
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyWrPhysicalBalanceCount;
            //            dblReorderPointQuantity = objItemTransactionDetail.QntyReorderPoint;
            //        }
            //        else
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyWrPhysicalBalanceCount / objItemTransactionDetail.PackQuantity;
            //            dblReorderPointQuantity = objItemTransactionDetail.QntyReorderPoint / objItemTransactionDetail.PackQuantity;
            //        }
            //        strMessage = MyApp.gLng.GS((int)MsgID, new object[]{
            //                                                 objItemTransactionDetail.WarehouseID.ToString(),
            //                                                 dblStockQuantity.ToString(),
            //                                                 objItemTransactionDetail.UnitOfSaleID,
            //                                                 objItemTransactionDetail.ItemID,
            //                                                 objItemTransactionDetail.Size.Description,
            //                                                 objItemTransactionDetail.Color.Description,
            //                                                 dblReorderPointQuantity.ToString()
            //                                                });
            //        break;

            //    default:
            //        if (objItemTransactionDetail.PackQuantity == 0)
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyAvailableBalanceCount;
            //        }
            //        else
            //        {
            //            dblStockQuantity = objItemTransactionDetail.QntyAvailableBalanceCount / objItemTransactionDetail.PackQuantity;
            //        }
            //        strMessage = MyApp.gLng.GS((int)MsgID, new object[]{
            //                                                 objItemTransactionDetail.WarehouseID.ToString(),
            //                                                 dblStockQuantity.ToString(),
            //                                                 objItemTransactionDetail.UnitOfSaleID,
            //                                                 objItemTransactionDetail.ItemID,
            //                                                 objItemTransactionDetail.Size.Description,
            //                                                 objItemTransactionDetail.Color.Description
            //                                                });
            //        break;
            //}
            //    if (!string.IsNullOrEmpty(strMessage))
            //    {
            //        MessageBox.Show(strMessage, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            //    }

        }


        public void Dispose()
        {
            headerEvents = null;
            detailEvents = null;
            if (bsoItemTrans != null)
            {
                bsoItemTrans.WarningItemStock -= BsoItemTrans_WarningItemStock;
                bsoItemTrans = null;
            }
            propChangeNotifier = null;
        }


        private TransactionID CreateNewDocumentTDE(string transSerial, Int16 wareHouseID, string transDoc, double discountAmount, double partyID, double salesmanID, bool newTransaction)
        {
            TransactionID insertedTrans = null;

            BSOItemTransaction bsoItemTransaction = null;
            //Inicialiizar o motor do documentos de venda
            bsoItemTransaction = new BSOItemTransaction();
            bsoItemTransaction.UserPermissions = MyApp.SystemSettings.User;


            try
            {
                BSOItemTransactionDetail BSOItemTransDetail = null;
                //'-------------------------------------------------------------------------
                //' DOCUMENT HEADER and initialization
                //'-------------------------------------------------------------------------
                //'*** Total source document amount. Save to verify at the end if an adjustment is necessary
                //'OriginalDocTotalAmount = 10
                //'
                // Documento
                if (!MyApp.SystemSettings.WorkstationInfo.Document.IsInCollection(transDoc))
                {
                    throw new Exception("O documento não se encontra preenchido ou não existe");
                }
                Document doc = MyApp.SystemSettings.WorkstationInfo.Document[transDoc];
                // Série
                if (!MyApp.SystemSettings.DocumentSeries.IsInCollection(transSerial))
                {
                    throw new Exception("A série não se encontra preenchida ou não existe");
                }
                DocumentsSeries series = MyApp.SystemSettings.DocumentSeries[transSerial];
                //if (series.SeriesType != SeriesTypeEnum.SeriesExternal) {
                //    throw new Exception("Para lançamentos de documentos externos à aplicação apenas são permitidas séries externas.");
                //}
                //
                var transType = DocumentTypeEnum.dcTypeSale;

                //
                // Motor do documento
                bsoItemTransaction.TransactionType = transType;
                // Motor dos detalhes (linhas)
                BSOItemTransDetail = new BSOItemTransactionDetail();
                BSOItemTransDetail.TransactionType = transType;
                // Utilizador e permissões
                BSOItemTransDetail.UserPermissions = MyApp.SystemSettings.User;
                BSOItemTransDetail.PermissionsType = FrontOfficePermissionEnum.foPermByUser;
                //
                bsoItemTransaction.BSOItemTransactionDetail = BSOItemTransDetail;
                BSOItemTransDetail = null;

                //
                //Inicializar uma transação
                bsoItemTransaction.Transaction = new ItemTransaction();
                if (newTransaction)
                {
                    bsoItemTransaction.InitNewTransaction(transDoc, transSerial);

                    bsoItemTransaction.UserPermissions = MyApp.SystemSettings.User;

                    ItemTransaction trans = bsoItemTransaction.Transaction;
                    if (trans == null)
                    {
                        if (newTransaction)
                        {
                            throw new Exception(string.Format("Não foi possivel inicializar o documento [{0}] da série [{1}]", transDoc, transSerial));
                        }
                        else
                        {
                            throw new Exception(string.Format("Não foi possivel carregar o documento [{0}] da série [{1}] número [{2}]", transDoc, transSerial));
                        }
                    }
                    //
                    // Limpar todas as linhas
                    int i = 1;
                    while (trans.Details.Count > 0)
                    {
                        trans.Details.Remove(ref i);
                    }
                    //
                    // Definir o terceiro (cliente ou fronecedor)
                    bsoItemTransaction.PartyID = partyID;
                    bsoItemTransaction.DefaultWarehouse = wareHouseID;
                    //
                    //Descomentar para indicar uma referência externa ao documento:
                    //trans.ContractReferenceNumber = ExternalDocId;
                    //
                    //Set Create date and deliverydate
                    var createDate = DateTime.Today;
                    trans.CreateDate = createDate;
                    trans.ActualDeliveryDate = createDate;
                    //
                    // Definir se o imposto é incluido
                    trans.TransactionTaxIncluded = true;
                    //
                    // Definir o pagamento. Neste caso optou-se por utilizar o primeiro pagamento disponivel na base de dados
                    short PaymentId = MyApp.DSOCache.PaymentProvider.GetFirstID();
                    trans.Payment = MyApp.DSOCache.PaymentProvider.GetPayment(PaymentId);
                    //
                    // Comentários / Observações
                    trans.Comments = "Comentários aqui!";
                    //
                    // Salesman
                    trans.Salesman.SalesmanID = salesmanID;

                    //
                    //-------------------------------------------------------------------------
                    // DOCUMENT DETAILS
                    //-------------------------------------------------------------------------
                    //
                    //Adicionar a primeira linha ao documento
                    double qty = 1;
                    double unitPrice = discountAmount;
                    double taxPercent = 23;
                    Item item = MyApp.DSOCache.ItemProvider.GetItem("Points", MyApp.SystemSettings.BaseCurrency);

                    if (item == null)
                    {
                        item = CreateItemPointsForTDE();
                    }

                    //
                    TransAddDetail(trans, item, "Promoção especial API Sage Retail", qty, "Uni", unitPrice, taxPercent, wareHouseID);


                    // Desconto Global -- Atribuir só no fim do documento depois de adicionadas todas as linhas
                    double globalDiscount = 0;
                    //double.TryParse(txtTransGlobalDiscount.Text, out globalDiscount);
                    bsoItemTransaction.PaymentDiscountPercent1 = globalDiscount;

                    //Calcular todo o documento
                    bsoItemTransaction.Calculate(true, true);
                    //
                    bsoItemTransaction.SaveDocument(false, false);

                    insertedTrans = bsoItemTransaction.Transaction.TransactionID;
                    //
                    BSOItemTransDetail = null;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }


            return insertedTrans;
        }

        private void TransAddDetail(ItemTransaction trans, Item item, string itemDescription, double qty, string unitOfMeasureId, double unitPrice, double taxPercent, short whareHouseId)
        {

            var doc = MyApp.SystemSettings.WorkstationInfo.Document[trans.TransDocument];

            ItemTransactionDetail transDetail = new ItemTransactionDetail();
            transDetail.BaseCurrency = MyApp.SystemSettings.BaseCurrency;
            transDetail.ItemID = item.ItemID;
            transDetail.CreateDate = trans.CreateDate;
            transDetail.CreateTime = trans.CreateTime;
            transDetail.ActualDeliveryDate = trans.CreateDate;
            //Utilizar a descrição do artigo, ou uma descrição personalizada
            transDetail.Description = itemDescription;

            // definir a quantidade
            transDetail.Quantity = qty;
            // Preço unitário. NOTA: Ver a diferença se o documento for com impostos incluidos!
            if (trans.TransactionTaxIncluded)
                transDetail.TaxIncludedPrice = unitPrice;
            else
                transDetail.UnitPrice = unitPrice;
            // Definir a lista de unidades
            transDetail.UnitList = item.UnitList;
            // Definir a unidade de venda/compra
            transDetail.SetUnitOfSaleID(unitOfMeasureId);
            //Definir os impostos
            short TaxGroupId = MyApp.DSOCache.TaxesProvider.GetTaxableGroupIDFromTaxRate(taxPercent, MyApp.SystemSettings.SystemInfo.DefaultCountryID, MyApp.SystemSettings.SystemInfo.TaxRegionID);
            transDetail.TaxableGroupID = TaxGroupId;
            //*** Uncomment for discout
            //transDetail.DiscountPercent = 10
            //
            // Se o Armazém não existir, utilizar o default que se encontra no documento.
            transDetail.WarehouseID = whareHouseId;
            // Identificador da linha
            transDetail.LineItemID = trans.Details.Count + 1;
            //
            //*** Uncomment to provide line totals
            //.TotalGrossAmount =        'Line Gross amount
            //.TotalNetAmount =          'Net Gross amount
            //
            //Definir o último preço de compra
            if (doc.TransDocType == DocumentTypeEnum.dcTypePurchase)
            {
                transDetail.ItemExtraInfo.ItemLastCostTaxIncludedPrice = item.SalePrice[0].TaxIncludedPrice;
                transDetail.ItemExtraInfo.ItemLastCostUnitPrice = item.SalePrice[0].UnitPrice;
            }

            item = null;
            //
            trans.Details.Add(transDetail);
        }


        private Item CreateItemPointsForTDE()
        {
            var newItem = new RTLBase16.Item();
            var dsoPriceLine = new RTLDL16.DSOPriceLine();
            newItem.ItemID = "Points";
            newItem.Description = "Artigo Points para a Extensibilidade";
            // IVA/Imposto por omissão do sistema
            newItem.TaxableGroupID = MyApp.SystemSettings.SystemInfo.DefaultTaxableGroupID;
            newItem.SupplierID = MyApp.DSOCache.SupplierProvider.GetFirstSupplierEx();
            //Inicializar as linhas de preço do artigo
            newItem.InitPriceList(dsoPriceLine.GetPriceLineRS());
            // Preço do artigo (linha de preço=1)
            RTLBase16.Price myPrice = newItem.SalePrice[1, 0];
            // Definir o preços (neste caso, com imposto (IVA) incluido)
            myPrice.TaxIncludedPrice = 0;
            // Obter preço unitário sem impostos
            myPrice.UnitPrice = 0;

            double familyId = MyApp.DSOCache.FamilyProvider.GetFirstLeafFamilyID();
            newItem.Family = MyApp.DSOCache.FamilyProvider.GetFamily(familyId);

            MyApp.DSOCache.ItemProvider.Save(newItem, newItem.ItemID, true);

            return newItem;
        }
    }
}