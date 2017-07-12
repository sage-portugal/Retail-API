﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using RTLSystem16;
using RTLUtil16;
using RTLBase16;
using RTLBL16;
using RTLDL16;
using RTLPrint16;

namespace Sage.Retail.API.Sample {
    public partial class fApi : Form {
        /// <summary>
        /// Motor de dados para os artigos.
        /// NOTA: Api tem de estar inicializada antes de usar!
        /// </summary>
        private DSOItem itemProvider { get { return RTLAPIEngine.DSOCache.ItemProvider; } }
        /// <summary>
        /// Parâmetros do sistema
        /// </summary>
        private SystemSettings systemSettings { get { return RTLAPIEngine.SystemSettings; } }
        /// <summary>
        /// Cache dos motores de acesso a dados mais comuns
        /// </summary>
        private DSOFactory dsoCache { get { return RTLAPIEngine.DSOCache; } }
        //
        /// <summary>
        /// Inidica que houve um erro na transação e não foi gravada
        /// </summary>
        private bool transactionError = false;
        /// <summary>
        /// Motor das transações de documentos de compra e venda
        /// </summary>
        private BSOItemTransaction bsoItemTransaction = null;
        /// <summary>
        /// Motor das transações de documentos de stock
        /// </summary>
        private BSOStockTransaction bsoStockTransaction = null;
        /// <summary>
        /// Motor das transações de recibos e pagamentos
        /// </summary>
        AccountTransactionManager accountTransManager = null;
        /// <summary>
        /// Printing MANAGER
        /// </summary>
        private PrintingManager printingManager { get { return RTLAPIEngine.PrintingManager; } }

        public fApi() {
            InitializeComponent();

            if( cboApplication.Items.Count > 0) {
                cboApplication.SelectedIndex = 0;
            }

            RTLAPIEngine.APIStarted += RTLAPIEngine_APIStarted;
            RTLAPIEngine.APIStopped += RTLAPIEngine_APIStopped;
        }

        #region Eventos da RTLAPI

        void RTLAPIEngine_APIStopped(object sender, EventArgs e) {
            accountTransManager = null;
            bsoItemTransaction = null;
            bsoStockTransaction = null;

            tabEntities.Enabled = false;
            btnStopAPI.Enabled = false;
            btnStartAPI.Enabled = true;

            btnInsert.Enabled = false;
            btnUpdate.Enabled = false;
            btnRemove.Enabled = false;
            btnGet.Enabled = false;
            btnClear.Enabled = false;

            cboApplication.Enabled = true;

            this.Cursor = Cursors.Default;
        }

        void RTLAPIEngine_APIStarted(object sender, EventArgs e) {
            tabEntities.Enabled = true;

            btnStopAPI.Enabled = true;
            btnStartAPI.Enabled = false;
            cboApplication.Enabled = false;

            btnInsert.Enabled = true;
            btnUpdate.Enabled = true;
            btnRemove.Enabled = true;
            btnGet.Enabled = true;
            btnClear.Enabled = true;
            btnPrint.Enabled = true;
            chkPrintPreview.Enabled = true;
            //
            btnAccoutTransPrint.Enabled = true;
            chkAccoutTransPrintPreview.Enabled = true;
            //
            //Inicialiizar o motor do documentos de venda
            bsoItemTransaction = new BSOItemTransaction();
            bsoItemTransaction.UserPermissions = systemSettings.User;
            //Eventos
            bsoItemTransaction.WarningItemStock += BsoItemTransaction_WarningItemStock;

            //
            //Inicializar o motor dos documentos de stock
            bsoStockTransaction = new BSOStockTransaction();
            bsoStockTransaction.UserPermissions = systemSettings.User;
            //
            // Inicilizar o motor dos recibos e pagamentos
            accountTransManager = new AccountTransactionManager();
            //
            // Load combos
            // Customer -- Load combos data and clear
            ItemClear( true );
            CustomerClear();
            SupplierClear();
            TransactionClear();
            AccountTransactionClear();

            //txtTransDoc.Text = "FAC";
            //txtTransSerial.Text = "1";
            //txtTransDocNumber.Text = "2";
            //chkPrintPreview.Checked = false;

            this.Cursor = Cursors.Default;
        }

        /// <summary>
        /// Mensagem de aviso de falta de stock, tal como está no RETAIL
        /// </summary>
        /// <param name="MsgID"></param>
        /// <param name="objItemTransactionDetail"></param>
        private void BsoItemTransaction_WarningItemStock(TransactionWarningsEnum MsgID, ItemTransactionDetail objItemTransactionDetail) {
            double dblStockQuantity = 0;
            double dblReorderPointQuantity = 0;
            string strMessage = string.Empty;

            switch ( MsgID) {
                case TransactionWarningsEnum.tweItemColorSizeStockNotHavePhysical:
                case TransactionWarningsEnum.tweItemStockNotHavePhysical:
                    if (objItemTransactionDetail.PackQuantity == 0) {
                        dblStockQuantity = objItemTransactionDetail.QntyPhysicalBalanceCount;
                    }
                    else {
                        dblStockQuantity = objItemTransactionDetail.QntyPhysicalBalanceCount / objItemTransactionDetail.PackQuantity;
                    }
                    strMessage = RTLAPIEngine.gLng.GS( (int)MsgID, new object[]{
                                                             objItemTransactionDetail.WarehouseID.ToString().Trim(),
                                                             dblStockQuantity,
                                                             objItemTransactionDetail.UnitOfSaleID,
                                                             objItemTransactionDetail.ItemID,
                                                             objItemTransactionDetail.Size.Description,
                                                             objItemTransactionDetail.Color.Description}
                                                     );

                    break;

                case TransactionWarningsEnum.tweItemReorderPoint:
                case TransactionWarningsEnum.tweItemColorSizeReorderPoint:
                    if (objItemTransactionDetail.PackQuantity == 0) {
                        dblStockQuantity = objItemTransactionDetail.QntyWrPhysicalBalanceCount;
                        dblReorderPointQuantity = objItemTransactionDetail.QntyReorderPoint;
                    }
                    else {
                        dblStockQuantity = objItemTransactionDetail.QntyWrPhysicalBalanceCount / objItemTransactionDetail.PackQuantity;
                        dblReorderPointQuantity = objItemTransactionDetail.QntyReorderPoint / objItemTransactionDetail.PackQuantity;
                    }
                    strMessage = RTLAPIEngine.gLng.GS((int)MsgID, new object[]{ 
                                                             objItemTransactionDetail.WarehouseID.ToString(),
                                                             dblStockQuantity.ToString(),
                                                             objItemTransactionDetail.UnitOfSaleID,
                                                             objItemTransactionDetail.ItemID,
                                                             objItemTransactionDetail.Size.Description,
                                                             objItemTransactionDetail.Color.Description,
                                                             dblReorderPointQuantity.ToString()}
                                );
                    break;

                default:
                    if (objItemTransactionDetail.PackQuantity == 0) {
                        dblStockQuantity = objItemTransactionDetail.QntyAvailableBalanceCount;
                    }
                    else {
                        dblStockQuantity = objItemTransactionDetail.QntyAvailableBalanceCount / objItemTransactionDetail.PackQuantity;
                    }
                    strMessage = RTLAPIEngine.gLng.GS((int)MsgID, new object[]{
                                                             objItemTransactionDetail.WarehouseID.ToString(),
                                                             dblStockQuantity.ToString(),
                                                             objItemTransactionDetail.UnitOfSaleID,
                                                             objItemTransactionDetail.ItemID,
                                                             objItemTransactionDetail.Size.Description,
                                                             objItemTransactionDetail.Color.Description}
                                                      );
                    break;
            }
            if(!string.IsNullOrEmpty( strMessage)) {
                MessageBox.Show(strMessage, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        void accountTransManager_FunctionExecuted(string FunctionName, string FunctionParam) {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Mensagens de AVISO da API
        /// Vamos mostrar só as mensagens
        /// </summary>
        /// <param name="Message"></param>
        void RTLAPIEngine_WarningMessage(string Message) {
            //Indicar um erro na transação de forma a cancelá-la
            transactionError = true;
            //
            MessageBox.Show(Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        /// <summary>
        /// Mensagens de erro da API
        /// Neste caso vamos lançar uma exeção que será apanhada no botão pressionado neste exemplo, de forma a informar o utilizador que falhou.
        /// </summary>
        /// <param name="Number">Número do erro </param>
        /// <param name="Source">O método que gerou o erro</param>
        /// <param name="Description">A descrição do erro</param>
        void RTLAPIEngine_WarningError(int Number, string Source, string Description) {
            //Indicar um erro na transação de forma a cancelá-la
            transactionError = true;
            //
            string msg = string.Format("Erro: {0}{1}Fonte: {2}{1}{3}", Number, Environment.NewLine, Source, Description);
            MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>
        /// Mensagens genéricas de AVISO/ERRO/INFO da API
        /// Vamos mostrar só as mensagens
        /// É necessário devolver um valor no Args.Result
        /// </summary>
        /// <param name="Message"></param>
        private void RTLAPIEngine_Message(RTLAPIEngine.MessageEventArgs Args) {
            Args.Result = MessageBox.Show(Args.Prompt, Args.Title, Args.Buttons, Args.Icon);
        }
        #endregion



        #region User interface
        /// <summary>
        /// Inicialização da API
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStartAPI_Click(object sender, EventArgs e) {
            try {
                this.Cursor = Cursors.WaitCursor;

                RTLAPIEngine.WarningError += RTLAPIEngine_WarningError;
                RTLAPIEngine.WarningMessage += RTLAPIEngine_WarningMessage;
                RTLAPIEngine.Message += RTLAPIEngine_Message;

                var apiKind = (RTLAPIEngine.ApplicationEnum)(cboApplication.SelectedIndex + 1);
                RTLAPIEngine.Initialize( apiKind, txtRTLCompany.Text, chkAPIDebugMode.Checked );
            }
            catch (Exception ex) {
                this.Cursor = Cursors.Default;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void fApi_FormClosed(object sender, FormClosedEventArgs e) {
            RTLAPIEngine.Terminate();
            Application.Exit();
        }

        private void btnCloseAPI_Click(object sender, EventArgs e) {
            if (RTLAPIEngine.APIInitialized) {
                RTLAPIEngine.Terminate();
            }
            Application.Exit();
        }

        #endregion



        #region ITEM

        /// <summary>
        /// Insere um novo artigo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnInsert_Click(object sender, EventArgs e) {
            try {
                transactionError = false;
                TransactionID transId = null;

                switch (tabEntities.SelectedIndex) {
                    case 0: ItemInsert(); break;
                    case 1: CustomerUpdate((double)numCustomerId.Value, true); break;
                    case 2: SupplierUpdate(double.Parse(txtSupplierId.Text), true); break;
                    case 3: transId = TransactionInsert(false); break;
                    case 4: transId = AccountTransactionUpdate(true); break;

                    case 5: UnitOfMeasureUpdate(txtUnitOfMeasureId.Text, true); break;
                }
                if (!transactionError) {
                    string msg = null;
                    if (transId != null) {
                        msg = string.Format("Registo inserido: {0}", transId.ToString());
                    }
                    else
                        msg = "Registo inserido.";

                    MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Remove um artigo da BD
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRemove_Click(object sender, EventArgs e) {
            try{
                if (DialogResult.Yes == MessageBox.Show("Anular este registo da base de dados?", Application.ProductName, MessageBoxButtons.YesNo, MessageBoxIcon.Question)) {
                    TransactionID transId = null;
                    transactionError = false;

                    switch (tabEntities.SelectedIndex) {
                        case 0: ItemRemove(); break;                                        //Artigos
                        case 1: CustomerRemove((double)numCustomerId.Value); break;         //Clientes
                        case 2: SupplierRemove( double.Parse(txtSupplierId.Text) ); break;  //Fornecedores
                        case 3: transId = TransactionRemove(); break;                                 //Compras e Vendas
                        case 4: transId = AccountTransactionRemove(); break;                          //Pagamentos e recebimentos

                        case 5: UnitOfMeasureRemove(txtUnitOfMeasureId.Text); break;        //Unidades de medida
                    }

                    if (!transactionError) {
                        string msg = null;
                        if (transId != null) {
                            msg = string.Format("Registo anulado: {0}", transId.ToString());
                        }
                        else {
                            msg = "Registo anulado.";
                        }
                        MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch( Exception ex ){
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }


        /// <summary>
        /// Altera um artigo
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAlterar_Click(object sender, EventArgs e) {
            try{
                TransactionID transId = null;
                transactionError = false;

                switch (tabEntities.SelectedIndex) {
                    case 0: ItemUpdate( txtItemId.Text ); break;
                    case 1: CustomerUpdate( (double)numCustomerId.Value, false ); break;
                    case 2: SupplierUpdate(double.Parse(txtSupplierId.Text), false); break;
                    case 3: transId = TransactionEdit(false); break;
                    case 4: transId = AccountTransactionUpdate(false); break;

                    case 5: UnitOfMeasureUpdate(txtUnitOfMeasureId.Text, false); break;
                }

                if (!transactionError) {
                    string msg = null;
                    if (transId != null) {
                        msg = string.Format("Registo alterado: {0}", transId.ToString());
                    }
                    else {
                        msg = "Registo alterado.";
                    }
                    MessageBox.Show(msg, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch( Exception ex ){
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }

        private void btnItemLoad_Click(object sender, EventArgs e) {
            try {
                switch (tabEntities.SelectedIndex) {
                    case 0: ItemGet( txtItemId.Text.Trim() ); break;
                    case 1: CustomerGet( (double)numCustomerId.Value ); break;
                    case 2: SupplierGet(double.Parse(txtSupplierId.Text)); break;
                    case 3: TransactionGet(false); break;
                    case 4: AccountTransactionGet(); break;

                    case 5: UnitOfMeasureGet( txtUnitOfMeasureId.Text ); break;
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        /// <summary>
        /// Cria um artigo novo
        /// * = Campos obrigatórios
        /// </summary>
        private void ItemInsert() {
            string itemId = txtItemId.Text.Trim();
            if (string.IsNullOrEmpty(itemId)) {
                MessageBox.Show("O código do artigo está vazio!", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            else {
                if( dsoCache.ItemProvider.ItemExist( itemId ) ){
                    throw new Exception(string.Format("O artigo [{0}] já existe.", itemId) );
                }

                var newItem = new Item();
                var dsoPriceLine = new DSOPriceLine();
                //*
                newItem.ItemID = itemId;
                newItem.Description = txtItemDescription.Text;
                newItem.ShortDescription = txtItemShortDescription.Text;
                newItem.Comments = txtItemComments.Text;
                // IVA/Imposto por omissão do sistema
                newItem.TaxableGroupID = systemSettings.SystemInfo.DefaultTaxableGroupID;
                //
                newItem.SupplierID = RTLAPIEngine.DSOCache.SupplierProvider.GetFirstSupplierEx();
                //
                //Inicializar as linhas de preço do artigo
                newItem.InitPriceList(dsoPriceLine.GetPriceLineRS());
                // Preço do artigo (linha de preço=1)
                Price myPrice = newItem.SalePrice[1, 0];
                //
                // Definir o preços (neste caso, com imposto (IVA) incluido)
                myPrice.TaxIncludedPrice = (double)numItemPriceTaxIncluded.Value;
                // Obter preço unitário sem impostos
                myPrice.UnitPrice = RTLAPIEngine.DSOCache.TaxesProvider.GetItemNetPrice(
                                                    myPrice.TaxIncludedPrice,
                                                    newItem.TaxableGroupID,
                                                    systemSettings.SystemInfo.DefaultCountryID,
                                                    systemSettings.SystemInfo.TaxRegionID);
                //
                // *Familia: Obter a primeira disponivel
                double familyId = RTLAPIEngine.DSOCache.FamilyProvider.GetFirstLeafFamilyID();
                newItem.Family = RTLAPIEngine.DSOCache.FamilyProvider.GetFamily(familyId);

                //// Descomentar para criar COR e adicionar ao artigo
                //// Criar nova côr na base de dados.
                //var newColorId = dsoCache.ColorProvider.GetNewID();
                //var colorCode = System.Drawing.Color.Blue.B << 32 + System.Drawing.Color.Blue.G << 16 + System.Drawing.Color.Blue.R;
                //var newColor = new RTLBase16.Color() {
                //    ColorCode = colorCode,
                //    ColorID = newColorId,
                //    Description = "Cor " + newColorId.ToString()
                //};
                //dsoCache.ColorProvider.Save(newColor, newColor.ColorID, true);
                ////
                //// Adicionar ao artigo
                //var newItemColor = new ItemColor() {
                //    ColorID = newColor.ColorID,
                //    ColorName = newColor.Description,
                //    ColorCode = (int)newColor.ColorCode,
                //    //ColorKey = NÃO USAR
                //};
                //newItem.Colors.Add(newItemColor);

                //// Descomentar para criar um novo tamanho e adicionar ao artigo
                //// Criar um tamanho nov
                //var newSizeID = dsoCache.SizeProvider.GetNewID();
                //var newSize = new RTLBase16.Size() {
                //    Description = "Size " + newSizeID.ToString(),
                //    SizeID = newSizeID,
                //    //SizeKey = NÃO USAR
                //};
                //dsoCache.SizeProvider.Save(newSize, newSize.SizeID, true);
                //var newItemSize = new ItemSize() {
                //    SizeID = newSize.SizeID,
                //    SizeName = newSize.Description,
                //    Quantity = 1,
                //    Units = 1
                //};
                //newItem.Sizes.Add(newItemSize);
                //
                // Gravar
                dsoCache.ItemProvider.Save(newItem, newItem.ItemID, true);
            }
        }

        /// <summary>
        /// Elimina um Artigo
        /// </summary>
        /// <param name="itemId"></param>
        private void ItemRemove() {
            string itemId = txtItemId.Text.Trim();
            itemProvider.Delete(itemId);
            //
            ItemClear(false);
        }

        /// <summary>
        /// Altera um Artigo
        /// </summary>
        /// <param name="itemId"></param>
        private void ItemUpdate( string itemId ) {
            var myItem = RTLAPIEngine.DSOCache.ItemProvider.GetItem(itemId, systemSettings.BaseCurrency);
            if (myItem != null) {
                myItem.Description = txtItemDescription.Text;
                myItem.ShortDescription = txtItemShortDescription.Text;
                myItem.Comments = txtItemComments.Text;
                //
                // Preços - PVP1
                Price myPrice = myItem.SalePrice[1, 0];
                // Definir o preço (neste caso, com imposto (IVA) incluido)
                myPrice.TaxIncludedPrice = (double)numItemPriceTaxIncluded.Value;
                // Obter preço unitário sem impostos
                myPrice.UnitPrice =RTLAPIEngine.DSOCache.TaxesProvider.GetItemNetPrice(
                                                    myPrice.TaxIncludedPrice,
                                                    myItem.TaxableGroupID,
                                                    systemSettings.SystemInfo.DefaultCountryID,
                                                    systemSettings.SystemInfo.TaxRegionID);
                //
                // Guardar as alterações
                RTLAPIEngine.DSOCache.ItemProvider.Save(myItem, myItem.ItemID, false);
            }
            else{
                throw new Exception( string.Format("Artigo [{0}] não encontrado.", itemId) );
            }
        }

        /// <summary>
        /// Ler e apresenta a informação de um artigo
        /// </summary>
        private void ItemGet(string itemId) {
            if (string.IsNullOrEmpty(itemId)) {
                throw new Exception("O código do artigo está vazio!");
            }
            else {
                //
                ItemClear(false);
                //Ler o artigo da BD na moeda base
                var item = itemProvider.GetItem(itemId, systemSettings.BaseCurrency);

                if (item != null) {
                    txtItemId.Text = item.ItemID;
                    txtItemDescription.Text = item.Description;
                    txtItemShortDescription.Text = item.ShortDescription;
                    numItemPriceTaxIncluded.Value = (decimal)item.SalePrice[1, 0].TaxIncludedPrice;
                    txtItemComments.Text = item.Comments;

                    cmbItemColor.DisplayMember = "ColorName";
                    cmbItemColor.ValueMember = "ColorID";
                    foreach (ItemColor value in item.Colors) {
                        cmbItemColor.Items.Add(value);
                    }

                    cmbItemSize.DisplayMember = "SizeName";
                    cmbItemSize.ValueMember = "SizeID";
                    foreach (ItemSize value in item.Sizes) {
                        cmbItemSize.Items.Add(value);
                    }
                }
                else {
                    throw new Exception(string.Format("O Artigo {0} não foi encontrado!", itemId));
                }
            }
        }

        /// <summary>
        /// Limpar o form
        /// </summary>
        private void ItemClear( bool clearItemId ) {
            //Limpar
            if( clearItemId )
                txtItemId.Text = string.Empty;
            txtItemDescription.Text = string.Empty;
            txtItemShortDescription.Text = string.Empty;
            numItemPriceTaxIncluded.Value = 0;
            txtItemComments.Text = string.Empty;
        }
        #endregion


        #region CUSTOMER
        /// <summary>
        /// Gravar (inserir ou alterar) um cliente
        /// </summary>
        /// <param name="customerId"></param>
        private void CustomerUpdate( double customerId, bool isNew) {
            Customer myCustomer = null;

            //Ler da BD se não for novo
            myCustomer = dsoCache.CustomerProvider.GetCustomer( customerId);
            if (myCustomer == null && !isNew) {
                throw new Exception(string.Format("O cliente [{0}] não existe.", customerId));
            }
            else if (myCustomer != null && isNew) {
                throw new Exception(string.Format("O cliente [{0}] já existe.", customerId));
            }

            if( myCustomer == null ){
                // Cliente NOVO
                // Obter um novo Id
                myCustomer = new Customer();
                myCustomer.CustomerID = (double)numCustomerId.Value;
            }
            
            myCustomer.OrganizationName = txtCustomerName.Text;
            myCustomer.FederalTaxId = txtCustomerTaxId.Text;
            myCustomer.Comments = txtCustomerComments.Text;
            //
            if (cmbCustomerTax.SelectedItem != null){
                var entityFiscalStatus = (EntityFiscalStatus)cmbCustomerTax.SelectedItem;
                myCustomer.EntityFiscalStatusID =  entityFiscalStatus.EntityFiscalStatusID;
            }
            myCustomer.SalesmanId = (int)numCustomerSalesmanId.Value;
            if( cmbCustomerCurrency.SelectedValue != null )
                myCustomer.CurrencyID = (string)cmbCustomerCurrency.SelectedValue;
            myCustomer.ZoneID = (short)numCustomerZoneId.Value;
            if( cmbCustomerCountry.SelectedItem != null )
                myCustomer.CountryID = ((CountryCode)cmbCustomerCountry.SelectedItem).CountryID;
            //
            // Outros campos obrigatórios
            myCustomer.CarrierID = dsoCache.CarrierProvider.GetFirstCarrierID();
            myCustomer.TenderID = dsoCache.TenderProvider.GetFirstTenderCash();
            myCustomer.CurrencyID = cmbCustomerCurrency.Text;
       
            // Se a zone estiver vazia, considerar a primeira zona nacional
            if( myCustomer.ZoneID == 0 )
                myCustomer.ZoneID = dsoCache.ZoneProvider.FindZone(ZoneTypeEnum.ztNational);
            // Se o modo de pagamento estiver vazio, obter o primeiro disponivel
            if( myCustomer.PaymentID == 0  ){
                myCustomer.PaymentID = dsoCache.PaymentProvider.GetFirstID();
            }
            // Se o vendedor não existir, utilizar o primeiro disponivel
            if (!dsoCache.SalesmanProvider.SalesmanExists(myCustomer.SalesmanId)) {
                myCustomer.SalesmanId = (int)dsoCache.SalesmanProvider.GetFirstSalesmanID();
            }
            // Se o pais não existir, rectificar
            if (!dsoCache.CountryProvider.CountryExists(myCustomer.CountryID))
                myCustomer.CountryID = systemSettings.SystemInfo.DefaultCountryID;
            // Se a moeda não existir, guar a moeda base
            if (!dsoCache.CurrencyProvider.CurrencyExists(myCustomer.CurrencyID))
                myCustomer.CurrencyID = systemSettings.BaseCurrency.CurrencyID;
            
            // Gravar. Se for novo NewRec = true;
            dsoCache.CustomerProvider.Save(myCustomer, myCustomer.CustomerID, isNew);
            //
            CustomerClear();
        }

        /// <summary>
        /// Ler um cliente da base de dados e apresentá-lo no ecran
        /// </summary>
        /// <param name="customerId"></param>
        private void CustomerGet(double customerId) {
            CustomerClear();
            var customer = dsoCache.CustomerProvider.GetCustomer(customerId);
            if (customer != null) {
                numCustomerId.Value = (decimal)customerId;
                numCustomerSalesmanId.Value = customer.SalesmanId;
                numCustomerZoneId.Value = customer.ZoneID;

                cmbCustomerCountry.SelectedItem = cmbCustomerCountry.Items.Cast<CountryCode>().FirstOrDefault(x=>x.CountryID == customer.CountryID);
                cmbCustomerCurrency.SelectedItem = cmbCustomerCurrency.Items.Cast<CurrencyDefinition>().FirstOrDefault(x => x.CurrencyID == customer.CurrencyID);
                cmbCustomerTax.SelectedItem = cmbCustomerTax.Items.Cast<EntityFiscalStatus>().FirstOrDefault(x => x.EntityFiscalStatusID == customer.EntityFiscalStatusID);
                
                txtCustomerComments.Text = customer.Comments;
                txtCustomerName.Text = customer.OrganizationName;
                txtCustomerTaxId.Text = customer.FederalTaxId;
            }
            else {
                //O cliente não existe!
                throw new Exception(string.Format("O Cliente {0} não foi encontrado!", customerId));
            }
        }

        /// <summary>
        /// Apagar um cliente
        /// </summary>
        /// <param name="customerId"></param>
        private void CustomerRemove( double customerId ) {
            dsoCache.CustomerProvider.Delete(customerId);
            CustomerClear();
        }


        private void CustomerClear() {
            // Obter um novo ID (para um novo cliente)
            numCustomerId.Value = (decimal)dsoCache.CustomerProvider.GetNewId();
            //
            txtCustomerComments.Text = string.Empty;
            txtCustomerName.Text = string.Empty;
            txtCustomerTaxId.Text = string.Empty;
            numCustomerSalesmanId.Value = 0;

            UIUtils.FillCountryCombo(cmbCustomerCountry);
            var country = cmbCustomerCountry.Items.Cast<CountryCode>()
                                            .FirstOrDefault(x => x.CountryID.Equals(systemSettings.SystemInfo.DefaultCountryID, StringComparison.CurrentCultureIgnoreCase));
            cmbCustomerCountry.SelectedItem = country;
            //
            UIUtils.FillCurrencyCombo(cmbCustomerCurrency);
            var currency = cmbCustomerCurrency.Items.Cast<CurrencyDefinition>()
                                              .FirstOrDefault(x => x.CurrencyID.Equals(systemSettings.BaseCurrency.CurrencyID, StringComparison.CurrentCultureIgnoreCase));
            cmbCustomerCurrency.SelectedItem = currency;
            //
            UIUtils.FillEntityFiscalStatusCombo(cmbCustomerTax);
        }
        
        #endregion


        #region SUPPLIER

        private void SupplierGet(double supplierId) {
            var supplier = dsoCache.SupplierProvider.GetSupplier(supplierId);
            if (supplier != null) {
                txtSupplierComments.Text = supplier.Comments;
                txtSupplierCountry.Text = supplier.CountryID;
                txtSupplierCurrency.Text = supplier.CurrencyID;
                txtSupplierId.Text = supplier.SupplierID.ToString();
                txtSupplierName.Text = supplier.OrganizationName;
                txtSupplierTaxId.Text = supplier.FederalTaxId;
                txtSupplierTax.Text = supplier.EntityFiscalStatusID.ToString();
                txtSupplierZone.Text = supplier.ZoneID.ToString();
            }
            else {
                SupplierClear();
            }

        }

        private void SupplierUpdate(double supplierId, bool isNew) {
            Supplier supplier = null;

            if( isNew && dsoCache.SupplierProvider.SupplierExists( supplierId ) ){
                throw new Exception(string.Format("O fornecedor [{0}] já existe.", supplierId));
            }
            if (!isNew) {
                supplier = dsoCache.SupplierProvider.GetSupplier(supplierId);
                if (supplier == null && !isNew) {
                    throw new Exception(string.Format("O fornecedor [{0}] não existe.", supplierId));
                }
            }
            //
            if (supplier == null) {
                // Como o fornecedor não existe na base de dados, vamos criar um novo
                supplier = new Supplier();
                supplier.SupplierID = supplierId;
            }
            supplier.Comments = txtSupplierComments.Text;
            supplier.CountryID = txtSupplierCountry.Text;
            supplier.CurrencyID = txtSupplierCurrency.Text;
            supplier.OrganizationName = txtSupplierName.Text;
            supplier.FederalTaxId = txtSupplierTaxId.Text;
            supplier.EntityFiscalStatusID = short.Parse(txtSupplierTax.Text);
            supplier.ZoneID = short.Parse(txtSupplierZone.Text);
            //
            //  A forma de pagamento é obrigatória. Vamos usar a primeira disponivel.
            supplier.PaymentID = dsoCache.PaymentProvider.GetFirstID();
            //  O meio de pagamento é obrigatório. VAmos usar o primeiro disponivel em numerário.
            supplier.TenderID = dsoCache.TenderProvider.GetFirstTenderCash();
            
            dsoCache.SupplierProvider.Save(supplier, supplier.SupplierID, isNew);

            SupplierClear();
        }


        private void SupplierRemove(double supplierId) {
            dsoCache.SupplierProvider.Delete(supplierId);
            SupplierClear();
        }

        private void SupplierClear() {
            txtSupplierComments.Text = string.Empty;
            txtSupplierCountry.Text = systemSettings.SystemInfo.DefaultCountryID;
            txtSupplierCurrency.Text = systemSettings.BaseCurrency.CurrencyID;
            txtSupplierId.Text = dsoCache.SupplierProvider.GetNewId().ToString();
            txtSupplierName.Text = string.Empty;
            txtSupplierTaxId.Text = "0";
            txtSupplierTax.Text = systemSettings.SystemInfo.DefaultTaxableGroupID.ToString();
            txtSupplierZone.Text = dsoCache.ZoneProvider.GetFirstID().ToString();
        }

        #endregion


        #region Unit of measure

        private void UnitOfMeasureGet(string unitOfMeasureId) {
            UnitOfMeasureClear();
            var unit = dsoCache.UnitOfMeasureProvider.GetUnitOfMeasure(unitOfMeasureId);
            if (unit != null) {
                txtUnitOfMeasureId.Text = unitOfMeasureId;
                txtUnitOfMeasureName.Text = unit.Description;
            }

        }

        private void UnitOfMeasureUpdate(string unitOfMeasureId, bool isNew) {
            UnitOfMeasure myUnit = null;
            if (!isNew) {
                myUnit = dsoCache.UnitOfMeasureProvider.GetUnitOfMeasure(unitOfMeasureId);
            }
            if (myUnit == null && !isNew) {
                throw new Exception(string.Format("A unidade de medida [{0}] não existe.", unitOfMeasureId));
            }
            if( myUnit == null ){
                myUnit = new UnitOfMeasure();
                myUnit.UnitOfMeasureID = unitOfMeasureId;
            }
            myUnit.Description = txtUnitOfMeasureName.Text;
            dsoCache.UnitOfMeasureProvider.Save(myUnit, myUnit.UnitOfMeasureID, isNew);

            UnitOfMeasureClear();
        }


        private void UnitOfMeasureRemove(string unitOfMeasureId) {
            dsoCache.UnitOfMeasureProvider.Delete(unitOfMeasureId);
            UnitOfMeasureClear();
        }

        private void UnitOfMeasureClear() {
            // Obter o próximo ID
            txtUnitOfMeasureId.Text = string.Empty;
            txtUnitOfMeasureName.Text = string.Empty;
        }

        #endregion


        #region Buy/Sale TRANSACTION

        private TransactionID TransactionRemove() {
            TransactionID transId = null;
            string transDoc = txtTransDoc.Text;
            string transSerial = txtTransSerial.Text;
            double transDocNumber = 0;
            double.TryParse(txtTransDocNumber.Text, out transDocNumber);
            bool result = false;

            var transType = TransGetType();

            if (rbTransBuySell.Checked) {
                if( transType != DocumentTypeEnum.dcTypeSale && transType != DocumentTypeEnum.dcTypePurchase ){
                    throw new Exception("O documento indicado não é um documento de compra ou venda.");
                }
                if (bsoItemTransaction.LoadItemTransaction(transType, transSerial, transDoc, transDocNumber)) {
                    // O motivo de anulação deve ser sempre preenchido.
                    // Se for obrigatório, o documento não é anulado sem que esteja preenchido
                    bsoItemTransaction.Transaction.VoidMotive = "Anulado por: " + Application.ProductName;
                    //
                    result = bsoItemTransaction.DeleteItemTransaction(false);
                    if (result) 
                        transId = bsoItemTransaction.Transaction.TransactionID;
                    else
                        throw new Exception(string.Format("Não foi possivel anular o documento {0} {1}/{2}", transDoc, transSerial, transDocNumber));
                }
                else
                    throw new Exception(string.Format("Não foi possivel carregar o documento {0} {1}/{2}.", transDoc, transSerial, transDocNumber));
            }
            else {
                if (transType != DocumentTypeEnum.dcTypeStock ) {
                    throw new Exception("O documento indicado não é um documento de stock.");
                }
                var loaded = bsoStockTransaction.LoadStockTransaction(transType, transSerial, transDoc, transDocNumber);
                if (loaded) {
                    // O motivo de anulação deve ser sempre preenchido.
                    // Se for obrigatório, o documento não é anulado sem que esteja preenchido
                    bsoStockTransaction.Transaction.VoidMotive = "Anulado por: " + Application.ProductName;
                    //
                    result = bsoStockTransaction.DeleteStockTransaction();
                    if (result) {
                        transId = new TransactionID();
                        transId.TransSerial = transSerial;
                        transId.TransDocument = transDoc;
                        transId.TransDocNumber = transDocNumber;
                    }
                    else
                        throw new Exception(string.Format("Não foi possivel anular o documento {0} {1}/{2}", transDoc, transSerial, transDocNumber));
                }
                else {
                    throw new Exception("O documento indicado não existe.");
                }
            }
            return transId;
        }


        /// <summary>
        /// Inserir ou Actualizar uma transação na base dados
        /// </summary>
        /// <returns></returns>
        private TransactionID TransactionInsert( bool suspendTransaction ) {
            string transDoc = txtTransDoc.Text;
            string transSerial = txtTransSerial.Text;
            double transDocNumber = 0;
            double.TryParse(txtTransDocNumber.Text, out transDocNumber);

            TransactionID result = null;
            if( rbTransBuySell.Checked )
                result = TransactionUpdate(transSerial, transDoc, transDocNumber, true, suspendTransaction);
            else
                result = TransactionStockUpdate(transSerial, transDoc, transDocNumber, true );
            
            return result;
        }

        private TransactionID TransactionEdit(bool suspendedTransaction) {
            string transDoc = txtTransDoc.Text;
            string transSerial = txtTransSerial.Text;
            double transDocNumber = 0;
            double.TryParse(txtTransDocNumber.Text, out transDocNumber);

            TransactionID result = null;
            if( rbTransBuySell.Checked )
                result = TransactionUpdate(transSerial, transDoc, transDocNumber, false, suspendedTransaction);
            else
                result = TransactionStockUpdate(transSerial, transDoc, transDocNumber, false);
            return result;
        }

        
        /// <summary>
        /// Insere ou altera uma transação (compra/venda)
        /// </summary>
        /// <param name="transSerial">Série</param>
        /// <param name="transDoc">Documento</param>
        /// <param name="transDocNumber">Número do documento</param>
        /// <param name="newTransaction">true: Nova transação (inserir); false: transação existente (alterar)</param>
        /// <returns>TransactionId da transação inserida/alterada</returns>
        private TransactionID TransactionUpdate( string transSerial, string transDoc, double transDocNumber, bool newTransaction, bool suspendTransaction ) {
            TransactionID insertedTrans = null;
            transactionError = false;

            try {
                BSOItemTransactionDetail BSOItemTransDetail = null;
                 
                //'-------------------------------------------------------------------------
                //' DOCUMENT HEADER and initialization
                //'-------------------------------------------------------------------------
                //'*** Total source document amount. Save to verify at the end if an adjustment is necessary
                //'OriginalDocTotalAmount = 10
                //'
                // Documento
                if (!systemSettings.WorkstationInfo.Document.IsInCollection(transDoc)) {
                    throw new Exception("O documento não se encontra preenchido ou não existe");
                }
                Document doc = systemSettings.WorkstationInfo.Document[transDoc];
                // Série
                if (!systemSettings.DocumentSeries.IsInCollection(transSerial)) {
                    throw new Exception("A série não se encontra preenchida ou não existe");
                }
                DocumentsSeries series = systemSettings.DocumentSeries[transSerial];
                //if (series.SeriesType != SeriesTypeEnum.SeriesExternal) {
                //    throw new Exception("Para lançamentos de documentos externos à aplicação apenas são permitidas séries externas.");
                //}
                //
                var transType = TransGetType();
                if (transType != DocumentTypeEnum.dcTypeSale && transType != DocumentTypeEnum.dcTypePurchase) {
                    throw new Exception(string.Format("O documento indicado [{0}] não é um documento de venda/compra", transDoc));
                }
                //
                if (!newTransaction && !suspendTransaction) {
                    //Exemplo: Verificar se uma transação existe:
                    if (!dsoCache.ItemTransactionProvider.ItemTransactionExists(doc.TransDocType, transSerial, transDoc, transDocNumber)) {
                        throw new Exception( string.Format("O documento {0} {1}/{2} não existe para ser alterado. Deve criar um novo.", transDoc, transSerial, transDocNumber));
                    }
                }
                //
                // Motor do documento
                bsoItemTransaction.TransactionType = transType;
                // Motor dos detalhes (linhas)
                BSOItemTransDetail = new BSOItemTransactionDetail();
                BSOItemTransDetail.TransactionType = transType;
                // Utilizador e permissões
                BSOItemTransDetail.UserPermissions = systemSettings.User;
                BSOItemTransDetail.PermissionsType = FrontOfficePermissionEnum.foPermByUser;
                //
                bsoItemTransaction.BSOItemTransactionDetail = BSOItemTransDetail;
                BSOItemTransDetail = null;
                //
                // Terceiro
                double partyId = 0;
                double.TryParse(txtTransPartyId.Text, out partyId);
                //
                //Inicializar uma transação
                bsoItemTransaction.Transaction = new ItemTransaction();
                if (newTransaction) {
                    bsoItemTransaction.InitNewTransaction(transDoc, transSerial);
                    if (transDocNumber > 0) {
                        // Tentar numeração indicada
                        bsoItemTransaction.Transaction.TransDocNumber = transDocNumber;
                    }
                }
                else {
                    if (suspendTransaction) {
                        //NOTA:
                        // transDocNumber=número da transação suspensa. Não número final
                        if ( ! bsoItemTransaction.LoadSuspendedTransaction(transSerial, transDoc, transDocNumber) ) {
                            throw new Exception(string.Format("O documento {0} {1}/{2} não existe para ser alterado. Deve criar um novo.", transDoc, transSerial, transDocNumber));
                        }
                    }
                    else {
                        bsoItemTransaction.LoadItemTransaction(transType, transSerial, transDoc, transDocNumber);
                    }
                }
                bsoItemTransaction.UserPermissions = systemSettings.User;

                ItemTransaction trans = bsoItemTransaction.Transaction;
                if (trans == null) {
                    if (newTransaction) {
                        throw new Exception(string.Format("Não foi possivel inicializar o documento [{0}] da série [{1}]", transDoc, transSerial));
                    }
                    else {
                        throw new Exception(string.Format("Não foi possivel carregar o documento [{0}] da série [{1}] número [{2}]", transDoc, transSerial, transDocNumber));
                    }
                }
                //
                // Limpar todas as linhas
                int i = 1;
                while (trans.Details.Count > 0) {
                    trans.Details.Remove(ref i);
                }
                //
                //// Definir o terceiro (cliente ou fornecedor)
                bsoItemTransaction.PartyID = partyId;
                //bsoItemTransaction.PartyFederalTaxID = "123456789";
                //bsoItemTransaction.PartyAddressLine1 = "Rua 1";
                //bsoItemTransaction.PartyPostalCode = "4000 Porto";
                //                
                //
                //Descomentar para indicar uma referência externa ao documento:
                //trans.ContractReferenceNumber = ExternalDocId;
                //
                //Set Create date and deliverydate
                var createDate = DateTime.Today;
                DateTime.TryParse(txtTransDate.Text, out createDate);
                trans.CreateDate = createDate;
                trans.ActualDeliveryDate = createDate;
                //
                // Definir se o imposto é incluido
                trans.TransactionTaxIncluded = chkTransTaxIncluded.Checked;
                //
                // Definir o pagamento. Neste caso optou-se por utilizar o primeiro pagamento disponivel na base de dados

                short PaymentId = 0;
                short.TryParse(txtPaymentID.Text, out PaymentId);
                if (PaymentId == 0) {
                    PaymentId = dsoCache.PaymentProvider.GetFirstID();
                }
                trans.Payment = dsoCache.PaymentProvider.GetPayment(PaymentId);
                //
                //*** Locais de carga e descarga
                //// Descomentar o seguinte para carregar um local de descarga "livre"
                //var placeId = RTLAPIEngine.DSOCache.LoadUnloadPlaceProvider.FindForAddressType(LoadUnloadAddressTypes.luatFree);
                //if( placeId == 0) {
                //    // Não existe nenhum local de carga/descar com endereços livres, por isso vamos criar um
                //    var freePlace = new LoadUnloadPlace() {
                //        AddressType = LoadUnloadAddressTypes.luatFree,
                //        Description = "Livre",
                //        LoadUnloadPlaceID = RTLAPIEngine.DSOCache.LoadUnloadPlaceProvider.GetNewID()
                //    };
                //    RTLAPIEngine.DSOCache.LoadUnloadPlaceProvider.Save(freePlace, freePlace.LoadUnloadPlaceID, true);
                //    placeId = freePlace.LoadUnloadPlaceID;
                //}
                ////
                //// Vamos definir o local de descarga
                //bsoItemTransaction.UnloadPlaceID = placeId;
                //bsoItemTransaction.Transaction.UnloadPlaceAddress.AddressLine1 = "Edifício Olympus II ";
                //bsoItemTransaction.Transaction.UnloadPlaceAddress.AddressLine2 = "Avenida D. Afonso Henriques, 1462 - 2º";
                //bsoItemTransaction.Transaction.UnloadPlaceAddress.PostalCode = "4450-013 Matosinhos";
                ////// Para local de carga, usar
                ////bsoItemTransaction.LoadPlaceID = placeId;
                ////bsoItemTransaction.Transaction.LoadPlaceAddress.AddressLine1 = "Edifício Olympus II ";
                ////bsoItemTransaction.Transaction.LoadPlaceAddress.AddressLine2 = "Avenida D. Afonso Henriques, 1462 - 2º";
                ////bsoItemTransaction.Transaction.LoadPlaceAddress.PostalCode = "4450-013 Matosinhos";
                //
                //Modo de Pagamento
                //se não preencheu nem tem cliente sugere o primeiro TenderID
                short tenderID = 0;
                short.TryParse(txtTenderID.Text, out tenderID);
                if (partyId == 0) {
                    if (tenderID == 0) {
                        tenderID = dsoCache.TenderProvider.GetFirstID();
                    }
                    trans.Tender.TenderID = tenderID;
                }
                
                //
                //ID da  Session
                short sessionID = RTLAPIEngine.SystemSettings.TillSession.SessionID;
               
                trans.WorkstationStamp.SessionID = sessionID;
                //

                if (txtTransCurrency.Text == "") {
                    trans.BaseCurrency = systemSettings.BaseCurrency;
                }
                else {
                    CurrencyDefinition currency = new CurrencyDefinition();
                    currency = dsoCache.CurrencyProvider.GetCurrency(txtTransCurrency.Text);
                    if (currency == null) {
                        throw new Exception(string.Format("A moeda[{0}] não existe.", txtTransCurrency.Text));
                    }
                    else {
                        trans.BaseCurrency = dsoCache.CurrencyProvider.GetCurrency(txtTransCurrency.Text);
                    }
                }
                //
                // Comentários / Observações
                trans.Comments = "Gerado por " + Application.ProductName;
                //
                //-------------------------------------------------------------------------
                // DOCUMENT DETAILS
                //-------------------------------------------------------------------------
                string itemId = txtItemId.Text;
                //
                //Adicionar a primeira linha ao documento
                double qty = 0; double.TryParse(txtTransQuantityL1.Text, out qty);
                double unitPrice = 0; double.TryParse(txtTransUnitPriceL1.Text, out unitPrice);
                double taxPercent = 0; double.TryParse(txtTransTaxRateL1.Text, out taxPercent);
                short wareHouseId = 0; short.TryParse(txtTransWarehouseL1.Text, out wareHouseId);
                Item item = TransGetCreateItem(txtTransItemL1.Text, string.Empty, string.Empty, txtTransUnL1.Text, string.Empty, 1, false, false, 0, 0, taxPercent);
                short colorId = 0;
                short.TryParse(txtTransColor1.Text, out colorId);
                short sizeId = 0;
                short.TryParse(txtTransSize1.Text, out sizeId);
                string serialNumber = txtTransPropValueL1.Text;
                string lotId = txtTransLotIdL1.Text;
                string lotDescription = txtTransLotEditionL1.Text;
                DateTime lotExpDate = DateTime.Now.AddYears(1);
                DateTime.TryParse(txtTransLotExpDateL1.Text, out lotExpDate);
                var currentDate = DateTime.Today;
                short lotRetWeek = GetWeekOfYear(currentDate);
                short.TryParse(txtTransLotRetWeekL1.Text, out lotRetWeek);
                var lotRetYear = (short)currentDate.Year;
                short.TryParse(txtTransLotRetYearL1.Text, out lotRetYear);
                short lotEditionId = 1;
                short.TryParse(txtTransLotEditionL1.Text, out lotEditionId);
                //
                TransAddDetail(trans, item, qty, txtTransUnL1.Text, unitPrice, taxPercent, wareHouseId, colorId, sizeId, lblTransPropNameL1.Text, serialNumber, lotId, lotDescription, lotExpDate, lotRetWeek, lotRetYear, lotEditionId);
                //
                //Adicionar a segunda linha ao documento
                if (!string.IsNullOrEmpty(txtTransItemL2.Text)) {
                    qty = 0; double.TryParse(txtTransQuantityL2.Text, out qty);
                    unitPrice = 0; double.TryParse(txtTransUnitPriceL2.Text, out unitPrice);
                    taxPercent = 0; double.TryParse(txtTransTaxRateL2.Text, out taxPercent);
                    wareHouseId = 0; short.TryParse(txtTransWarehouseL2.Text, out wareHouseId);
                    item = TransGetCreateItem(txtTransItemL2.Text, string.Empty, string.Empty, txtTransUnL2.Text, string.Empty, 1, false, false, 0, 0, taxPercent);
                    colorId = 0;
                    short.TryParse(txtTransColor1.Text, out colorId);
                    sizeId = 0;
                    short.TryParse(txtTransSize1.Text, out sizeId);
                    serialNumber = txtTransPropValueL2.Text;
                    lotId = txtTransLotIdL2.Text;
                    lotDescription = txtTransLotEditionL2.Text;
                    lotExpDate = DateTime.Now.AddYears(1);
                    DateTime.TryParse(txtTransLotExpDateL2.Text, out lotExpDate);
                    currentDate = DateTime.Today;
                    lotRetWeek = GetWeekOfYear(currentDate);
                    short.TryParse(txtTransLotRetWeekL2.Text, out lotRetWeek);
                    lotRetYear = (short)currentDate.Year;
                    short.TryParse(txtTransLotRetYearL2.Text, out lotRetYear);
                    lotEditionId = 1;
                    short.TryParse(txtTransLotEditionL1.Text, out lotEditionId);
                    //
                    TransAddDetail(trans, item, qty, txtTransUnL2.Text, unitPrice, taxPercent, wareHouseId, colorId, sizeId, lblTransPropNameL2.Text, serialNumber, lotId, lotDescription, lotExpDate, lotRetWeek, lotRetYear, lotEditionId);
                }
                //*** Descomentar a linha seguinte para definir automaticamente as origens (conversão de um documento)
                //bsoItemTransaction.FillTransactionOrigins()

                // Desconto Global -- Atribuir só no fim do documento depois de adicionadas todas as linhas
                double globalDiscount = 0;
                double.TryParse(txtTransGlobalDiscount.Text, out globalDiscount);
                bsoItemTransaction.PaymentDiscountPercent1 = globalDiscount;

                //Calcular todo o documento
                bsoItemTransaction.Calculate(true, true);
                //
                //*** Descomentar o seguinte para ajustar o total do documento (arredondamentos)
                //double OriginalDocTotalAmount = 9999; //Ajustar para o valor do documento original
                //if( OriginalDocTotalAmount != 0 ){
                //    double TotalDiff = OriginalDocTotalAmount - bsoItemTransaction.Transaction.TotalAmount;
                //    if( TotalDiff != 0 ){
                //        bsoItemTransaction.Transaction.TotalAdjustmentAmount = TotalDiff;
                //        bsoItemTransaction.Transaction.TotalAmount = bsoItemTransaction.Transaction.TotalAmount + TotalDiff;
                //        bsoItemTransaction.Transaction.TotalTransactionAmount = bsoItemTransaction.Transaction.TotalTransactionAmount + TotalDiff;
                //    }
                //}
                //
                //// Exemplo de pagamento por cheque...
                //// Gerar a linha do pagamento
                //var tenderCheck = dsoCache.TenderProvider.GetFirstTenderType(TenderTypeEnum.tndCheck);
                //if ( tenderCheck != null ) {
                //    // Preencher o cheque
                //    TenderCheck tCheck = new TenderCheck() {
                //        BankID = "AAA",                             // Código do banco! TempItemTransaction decimal existir
                //        CheckAmount = trans.TotalTransactionAmount, // Pagar na totalidade
                //        CheckDeferredDate = trans.CreateDate,       // Data do cheque
                //        CheckSequenceNumber = "987654321",          // Númerom do cheque
                //        TillID = trans.Till.TillID,                 // Caixa
                //        Guid = Guid.NewGuid().ToString()            // Guid identificador do registo
                //    };
                //    // Preencher a linha do pagamento
                //    var tenderLine = new TenderLineItem();
                //    tenderLine.Amount = trans.TotalTransactionAmount;
                //    tenderLine.CreateDate = trans.CreateDate;
                //    tenderLine.PartyID = trans.PartyID;
                //    tenderLine.PartyTypeCode = trans.PartyTypeCode;
                //    tenderLine.Tender = tenderCheck;
                //    tenderLine.TenderCheck = tCheck;

                //    trans.TenderLineItem.Add(tenderLine);
                //}
                //
                //
                // Exemplo para registar a origem nas Notas de crédito e Notas de débito:
                if (doc.Nature.NatureID == TransactionNatureEnum.Sale_CreditNote || doc.Nature.NatureID == TransactionNatureEnum.Sale_DebitNote) {
                    var originTransId = new TransactionID();
                    originTransId.Init("1","FAC",1);
                    trans.OriginatingON = originTransId.ToString();
                }

                if (suspendTransaction) {
                    insertedTrans = bsoItemTransaction.SuspendCurrentTransaction();
                }
                else {
                    bsoItemTransaction.SaveDocument(false, false);
                    //
                    if (!transactionError)
                        insertedTrans = bsoItemTransaction.Transaction.TransactionID;
                }
                //
                BSOItemTransDetail = null;
            }
            catch (Exception ex) {
                throw ex;
            }
            finally {
                //Unsubscribe from event
                bsoItemTransaction.TenderIDChanged -= bsoItemTransaction_TenderIDChanged;
            }

            return insertedTrans;
        }

        void bsoItemTransaction_TenderIDChanged(ref short value) {
            MessageBox.Show("bsoItemTransaction_TenderIDChanged");
        }



        /// <summary>
        /// Obtém ou cria um artigo novo e devolve-o
        /// </summary>
        /// <param name="itemId">Código do artigo</param>
        /// <param name="EANBarcode">Código de barras do artigo para a criação pode ser vazio.</param>
        /// <param name="itemDescription">Descrição do artigo para a criação. Pode ser vazio.</param>
        /// <param name="unitId">Unidade para a criação pode ser vazio.</param>
        /// <param name="packUnitId">Unidade de grupo (pack) para a crição. Se fornecida, deve também ser indicado o fator (unitsPerPack)</param>
        /// <param name="unitsPerPack">Factor de agrupamento ou número de unidades por pack. Se não fornecido, indicar 1</param>
        /// <param name="isKg">Indica se é uma unidade de peso (Kg)</param>
        /// <param name="isPack">Indica se é um pack</param>
        /// <param name="supplierId">Identificado do fornecedor para a criação. Obrigatório para criar um artigo novo.</param>
        /// <param name="unitCostPrice">Custo por unidade do artigo</param>
        /// <param name="itemTaxPercent">Taxa de imposto do artigo</param>
        /// <returns></returns>
        private Item TransGetCreateItem(string itemId, string EANBarcode, string itemDescription, 
                                   string unitId, string packUnitId, int unitsPerPack, 
                                   bool isKg, bool isPack, 
                                   double supplierId, double unitCostPrice,
                                   double itemTaxPercent) {
            //Descomentar para fazer a pesquisa por código de barras, código do artigo e código do fornecedor
            //string strItemID = dsoCache.ItemProvider.ItemSearch(itemId, 0, 0);
            //if( string.IsNullOrEmpty(strItemID) ) {
            //    //Search by supplier code
            //    object objSupplierId = supplierId;
            //    strItemID = dsoCache.ItemProvider.GetItemBySuplierReorderID(itemId, ref objSupplierId );
            //}
            //
            // senão, pesquisar o artigo por referência
            Item oItem = dsoCache.ItemProvider.GetItem(itemId, systemSettings.BaseCurrency);
            //
            // Se o artigo não existir devolver uma exceção
            if (oItem == null) {
                throw new Exception(string.Format("O Artigo[{0}] não existe.", itemId));
            }
            //
            // OU
            // Descomentar o seguinte para criar automaticamente um artigo novo
            //// Unidades de volume e fator
            ////
            //bool bSaveItem = false;
            ////
            ////Conversion processing
            //if (!isKg) {
            //    bool bHasPosIdentity = false;
            //    foreach (POSIdentity posIdentity in oItem.POSIdentity) {
            //        if (string.Compare(posIdentity.UnitOfMeasure, unitId, true) == 0) {
            //            bHasPosIdentity = true;
            //            break;
            //        }
            //    }
            //    //
            //    //POS Identity por unidade (Sem código de barras)
            //    if (!bHasPosIdentity && isPack) {
            //        POSIdentity posIdentity = new POSIdentity();
            //        posIdentity.UnitOfMeasure = unitId;
            //        posIdentity.Quantity = unitsPerPack;
            //        posIdentity.CurrencyID = systemSettings.BaseCurrency.CurrencyID;
            //        posIdentity.CurrencyExchange = systemSettings.BaseCurrency.BuyExchange;
            //        posIdentity.CurrencyFactor = systemSettings.BaseCurrency.EuroConversionRate;
            //        posIdentity.Description = oItem.Description;    //Or "Product custom description"
            //        oItem.POSIdentity.Add(posIdentity);
            //        posIdentity = null;
            //        bSaveItem = true;
            //    }
            //    //
            //    if (string.IsNullOrEmpty(oItem.BarCode)) {
            //        // código de barras EAN do artigo
            //        // Descomentar as linhas seguintes para atribuir um código de barras ao artigo:
            //        //oItem.BarCode = "12345678980123";
            //        //bSaveItem = true;
            //    }
            //    else if (!string.IsNullOrEmpty(EANBarcode)) {
            //        //Add new barcode, if not on present item
            //        if (!oItem.POSIdentity.IsInCollection(EANBarcode, oItem.UnitOfSaleID) && string.Compare(oItem.BarCode, EANBarcode, true) != 0) {
            //            POSIdentity oPOSIdentity = new POSIdentity();
            //            oPOSIdentity.UnitOfMeasure = oItem.UnitOfSaleID;
            //            oPOSIdentity.Quantity = 1;
            //            oPOSIdentity.CurrencyID = systemSettings.BaseCurrency.CurrencyID;
            //            oPOSIdentity.CurrencyExchange = systemSettings.BaseCurrency.BuyExchange;
            //            oPOSIdentity.CurrencyFactor = systemSettings.BaseCurrency.EuroConversionRate;
            //            oPOSIdentity.Description = itemDescription;    //Or custom description
            //            oPOSIdentity.POSItemID = EANBarcode;
            //            oItem.POSIdentity.Add(oPOSIdentity);
            //            oPOSIdentity = null;
            //            bSaveItem = true;
            //        }
            //    }
            //}
            ////
            //bool bHasItemSupplier = false;
            //foreach (ItemSupplier itemSupplier in oItem.SupplierList) {
            //    if (itemSupplier.SupplierID == supplierId && string.Compare(itemSupplier.ReorderID, itemId, true) == 0) {
            //        bHasItemSupplier = true;
            //        break;
            //    }
            //}
            ////
            //if (!bHasItemSupplier) {
            //    ItemSupplier itemSupplier = new ItemSupplier();
            //    itemSupplier.SupplierID = supplierId;
            //    itemSupplier.SupplierName = dsoCache.SupplierProvider.GetSupplierName(supplierId);
            //    //
            //    itemSupplier.UnitOfMeasure = unitId;
            //    itemSupplier.ReorderID = itemId;     //Supplier Reference
            //    itemSupplier.CurrencyID = systemSettings.BaseCurrency.CurrencyID;
            //    itemSupplier.CurrencyExchange = systemSettings.BaseCurrency.BuyExchange;
            //    itemSupplier.CurrencyFactor = systemSettings.BaseCurrency.EuroConversionRate;
            //    //Definir aqui o preço de custo:
            //    itemSupplier.CostPrice = unitCostPrice;

            //    oItem.SupplierList.Add(itemSupplier);
            //    itemSupplier = null;

            //    bSaveItem = true;
            //}
            ////Definir a taxa de imposto
            //short TaxGroupId = dsoCache.TaxesProvider.GetTaxableGroupIDFromTaxRate(itemTaxPercent, systemSettings.SystemInfo.DefaultCountryID, systemSettings.SystemInfo.TaxRegionID);
            //if (oItem.TaxableGroupID != TaxGroupId) {
            //    oItem.TaxableGroupID = TaxGroupId;
            //    bSaveItem = true;
            //}
            ////
            ////Gravar o artigo
            //if (bSaveItem)
            //    dsoCache.ItemProvider.Save(oItem, oItem.ItemID, false);

            return oItem;
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
        private void TransAddDetail( ItemTransaction trans, Item item, double qty, string unitOfMeasureId, double unitPrice, double taxPercent, short whareHouseId,
                                     short colorId, short sizeId, 
                                     string serialNumberPropId, string serialNumberPropValue, 
                                     string lotId, string lotDescription, DateTime lotExpDate, short lotReturnWeek, short lotReturnYear, short lotEditionId ){

            var doc = systemSettings.WorkstationInfo.Document[trans.TransDocument];
            
            ItemTransactionDetail transDetail = new ItemTransactionDetail();
            
            //Moeda dos detalhes de  documento
            if (txtTransCurrency.Text==""){
                transDetail.BaseCurrency = systemSettings.BaseCurrency;
            }
            else {
                CurrencyDefinition currency = new CurrencyDefinition();
                currency = dsoCache.CurrencyProvider.GetCurrency(txtTransCurrency.Text);
                if (currency == null)
                {
                    throw new Exception(string.Format("A moeda[{0}] não existe.", txtTransCurrency.Text));
                }
                else { 
                    transDetail.BaseCurrency=dsoCache.CurrencyProvider.GetCurrency(txtTransCurrency.Text);
                }
            }
            //
            
            transDetail.ItemID = item.ItemID;
            transDetail.CreateDate = trans.CreateDate;
            transDetail.CreateTime = trans.CreateTime;
            transDetail.ActualDeliveryDate = trans.CreateDate;
            //Utilizar a descrição do artigo, ou uma descrição personalizada
            transDetail.Description = item.Description;
            // definir a quantidade
            transDetail.Quantity = qty;
            // Preço unitário. NOTA: Ver a diferença se o documento for com impostos incluidos!
            if( trans.TransactionTaxIncluded)
                transDetail.TaxIncludedPrice =unitPrice;
            else
                transDetail.UnitPrice = unitPrice;
            // Definir a lista de unidades
            transDetail.UnitList = item.UnitList;
            // Definir a unidade de venda/compra
            transDetail.SetUnitOfSaleID(unitOfMeasureId);
            //Definir os impostos
            short TaxGroupId = dsoCache.TaxesProvider.GetTaxableGroupIDFromTaxRate(taxPercent, systemSettings.SystemInfo.DefaultCountryID, systemSettings.SystemInfo.TaxRegionID);
            transDetail.TaxableGroupID = TaxGroupId;
            //*** Uncomment for discout
            //transDetail.DiscountPercent = 10
            //
            // Se o Armazém não existir, utilizar o default que se encontra no documento.
            if( dsoCache.WarehouseProvider.WarehouseExists(whareHouseId) )
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
            if( doc.TransDocType == DocumentTypeEnum.dcTypePurchase ){
                transDetail.ItemExtraInfo.ItemLastCostTaxIncludedPrice = item.SalePrice[0].TaxIncludedPrice;
                transDetail.ItemExtraInfo.ItemLastCostUnitPrice = item.SalePrice[0].UnitPrice;
            }
            // Cores e tamanhos
            if (systemSettings.SystemInfo.UseColorSizeItems && chkTransModuleSizeColor.Checked ) {
                // Cores
                if (item.Colors.Count > 0) {
                    ItemColor color = null;
                    if (colorId > 0 && item.Colors.IsInCollection(colorId)) {
                        color = item.Colors[ref colorId];
                    }
                    if(color == null ) {
                        throw new Exception( string.Format("A cor indicada [{0}] não existe.", colorId));
                    }
                    transDetail.Color.ColorID = colorId;
                    transDetail.Color.Description = color.ColorName;
                    transDetail.Color.ColorKey = color.ColorKey;
                    transDetail.Color.ColorCode = color.ColorCode;
                }
                //Tamanhos
                if (item.Sizes.Count > 0 && chkTransModuleSizeColor.Checked) {
                    ItemSize size = null;
                    if (sizeId > 0 && item.Sizes.IsInCollection(sizeId)) {
                        size = item.Sizes[sizeId];
                    }
                    if( size == null ){
                        throw new Exception( string.Format("O tamanho indicado [{0}] não existe.", sizeId));
                    }
                    transDetail.Size.Description = size.SizeName;
                    transDetail.Size.SizeID = size.SizeID;
                    transDetail.Size.SizeKey = size.SizeKey;
                }
            }
            //
            // Lotes - Edições
            // Verificar se estão ativados no sistema e se foram marcados no documento
            if (systemSettings.SystemInfo.UseKiosksItems && chkTransModuleLot.Checked
                && (item.ItemType == ItemTypeEnum.itmLot || item.ItemType == ItemTypeEnum.itmEdition) ) {
                ItemLot lot = null;
                if (item.LotList.Count > 0) {
                    // Validar se existe a Edição
                    // NOTA: Numa venda vamos sempre assumir que o lote registado na BD é que contém toda a informação relevante como a Validade, Semana e ano de decolução, etc...
                    //       Vamos procurar pelo lote + edição
                    lot = null;
                    foreach (ItemLot tempLot in item.LotList) {
                        if( tempLot.LotID == lotId && tempLot.EditionID == lotEditionId ){
                            lot = tempLot;
                            break;
                        }
                    }
                }
                // Se for uma compra adicionamos o lote
                if (lot == null && doc.TransDocType == DocumentTypeEnum.dcTypePurchase && doc.SignPurchaseReport == "+") {
                    // Adicionar ume novo...
                    lot = new ItemLot();
                    lot.EditionID = lotEditionId;
                    lot.ItemID = item.ItemID;
                    lot.LotID = lotId;
                    lot.ExpirationDate = lotExpDate;
                    lot.ReturnWeek = lotReturnWeek;
                    lot.ReturnYear = lotReturnYear;
                    lot.ItemLotDescription = item.Description;
                    lot.SupplierItemID = dsoCache.ItemProvider.GetItemSupplierID(item.ItemID, item.SupplierID);
                }
                if(lot==null){
                    throw new Exception(string.Format("O lote [{0}], Edição [{1}] não existe.", lotId, lotEditionId));
                }
                transDetail.Lot.BarCode = lot.BarCode;
                transDetail.Lot.EditionID = lot.EditionID;
                transDetail.Lot.EffectiveDate = lot.EffectiveDate;
                transDetail.Lot.ExpirationDate = lot.ExpirationDate;
                transDetail.Lot.ItemID = lot.ItemID;
                transDetail.Lot.ItemLotDescription = lot.ItemLotDescription;
                transDetail.Lot.LotID = lot.LotID;
                transDetail.Lot.ReturnWeek = lot.ReturnWeek;
                transDetail.Lot.ReturnYear = lot.ReturnYear;
                transDetail.Lot.SalePrice = lot.SalePrice;
                transDetail.Lot.SaveSalePrice = lot.SaveSalePrice;
                transDetail.Lot.SupplierItemID = lot.SupplierItemID;
            }
            //
            // Propriedades (números de série e lotes)
            // ATENÇÃO: As regras de verificação das propriedades não estão implementadas na API. Deve ser a aplicação a fazer todas as validações necessárias
            //          Como por exemplo a movimentação duplicada de números de série
            // Verificar se estão ativadas no sistema e se foram marcadas no documento
            if (systemSettings.SystemInfo.UsePropertyItems && chkTransModuleProps.Checked) {
                // O Artigo tem propriedades ?
                if (item.PropertyEnabled ) {
                    // NOTA: Para o exemplo atual apenas queremos uma propriedade definida no artigo com o ID1 = "NS".
                    //       Para outras propriedades e combinações, o código deve ser alterado em conformidade.
                    if ( item.PropertyID1.Equals("NS", StringComparison.CurrentCultureIgnoreCase)) {
                        transDetail.ItemProperties.ResetValues();
                        transDetail.ItemProperties.PropertyID1 = item.PropertyID1;
                        transDetail.ItemProperties.PropertyID2 = item.PropertyID2;
                        transDetail.ItemProperties.PropertyID3 = item.PropertyID3;
                        transDetail.ItemProperties.ControlMode = item.PropertyControlMode;
                        transDetail.ItemProperties.ControlType = item.PropertyControlType;
                        transDetail.ItemProperties.UseExpirationDate = item.PropertyUseExpirationDate;
                        transDetail.ItemProperties.UseProductionDate = item.PropertyUseProductionDate;
                        transDetail.ItemProperties.ExpirationDateControl = item.PropertyExpirationDateControl;
                        transDetail.ItemProperties.MaximumQuantity = item.PropertyMaximumQuantity;
                        transDetail.ItemProperties.UsePriceOnProp1 = item.UsePriceOnProp1;
                        transDetail.ItemProperties.UsePriceOnProp2 = item.UsePriceOnProp2;
                        transDetail.ItemProperties.UsePriceOnProp3 = item.UsePriceOnProp3;
                        //
                        transDetail.ItemProperties.PropertyValue1 = serialNumberPropValue;
                    }
                }
            }
            item = null;
            //
            trans.Details.Add(transDetail);
        }

        private PartyTypeEnum TransGetPartyType() {
            switch (cmbTransPartyType.SelectedIndex) {
                case 0: return PartyTypeEnum.ptSupplier;
                case 1: return PartyTypeEnum.ptCustomer;
                case 2: return PartyTypeEnum.ptNothing;
                
                default: return PartyTypeEnum.ptNothing;
            }
        }

        private DocumentTypeEnum TransGetType() {
            DocumentTypeEnum transType = DocumentTypeEnum.dcTypeNone;
            string transDoc = txtTransDoc.Text;

            if (systemSettings.WorkstationInfo.Document.IsInCollection(transDoc)) {
                var doc = systemSettings.WorkstationInfo.Document[transDoc];
                transType = doc.TransDocType;
            }
            return transType;
        }


        /// <summary>
        /// Carrega um documento da base de dados e apresenta-o no ecran
        /// </summary>
        private void TransactionGet(bool suspendedTransaction) {
            Document doc = null;
            string transDoc = txtTransDoc.Text;
            string transSerial = txtTransSerial.Text;
            double transDocNumber = 0;
            double.TryParse(txtTransDocNumber.Text, out transDocNumber);
    
            // trans pode ser SaleTransaction ou BuyTransaction
            // dynamic permite utilizar as propriedades como num 'object' do VB6, sem que o compilador valide propriedades e métodos no momento da compilação
            dynamic trans = null;

            if( systemSettings.WorkstationInfo.Document.IsInCollection(transDoc) ){
                doc = systemSettings.WorkstationInfo.Document[ transDoc];
            }
            if (doc == null) {
                throw new Exception(string.Format(" O documento [{0}] não existe.", transDoc));
            }

            if (suspendedTransaction) {
                if ( bsoItemTransaction.LoadSuspendedTransaction( transSerial, transDoc, transDocNumber ) ){
                    trans = bsoItemTransaction.Transaction;
                }
                else {
                    MessageBox.Show(string.Format("Não foi encontrada a transação em preparação: {0} {1}/{2}", transDoc, transSerial, transDocNumber), 
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else {
                switch (doc.TransDocType) {
                    case DocumentTypeEnum.dcTypeSale:
                    case DocumentTypeEnum.dcTypePurchase:
                        if (!bsoItemTransaction.LoadItemTransaction(doc.TransDocType, transSerial, transDoc, transDocNumber)) {
                            throw new Exception(string.Format("Não foi possivel ler o documento [{0} {1}/{2}]", transDoc, transSerial, transDocNumber));
                        }
                        trans = bsoItemTransaction.Transaction;
                        rbTransBuySell.Checked = true;
                        break;

                    case DocumentTypeEnum.dcTypeStock:
                        if (!bsoStockTransaction.LoadStockTransaction(doc.TransDocType, transSerial, transDoc, transDocNumber)) {
                            throw new Exception(string.Format("Não foi possivel ler o documento [{0} {1}/{2}]", transDoc, transSerial, transDocNumber));
                        }
                        trans = bsoStockTransaction.Transaction;
                        rbTransStock.Checked = true;
                        break;

                    default:
                        throw new Exception(string.Format(" O documento [{0}] é de um tipo não suportado por este exemplo: {1}.", transDoc, doc.TransDocType));
                }
            }

            if (trans != null) {
                TransactionClear();
                //txtTransColor1.Text = 
                txtTransCurrency.Text = trans.BaseCurrency.CurrencyID;
                txtTransDate.Text = trans.CreateDate.ToShortDateString();
                txtTransDoc.Text = trans.TransDocument;
                txtTransDocNumber.Text = trans.TransDocNumber.ToString();
                txtPaymentID.Text = trans.Payment.PaymentID.ToString();
                txtTenderID.Text = trans.Tender.TenderID.ToString();
                //
                //ItemTransaction i; i.PaymentDiscountPercent
                if (doc.TransDocType == DocumentTypeEnum.dcTypeSale || doc.TransDocType == DocumentTypeEnum.dcTypePurchase) {
                    txtTransGlobalDiscount.Text = trans.PaymentDiscountPercent.ToString();
                    txtTransGlobalDiscount.Enabled = true;
                }
                else {
                    txtTransGlobalDiscount.Text = string.Empty;
                    txtTransGlobalDiscount.Enabled = false;
                }
                txtTransPartyId.Text = trans.PartyID.ToString();
                txtTransSerial.Text = trans.TransSerial;
                //
                //Linha 1
                if( trans.Details.Count>0){
                    var transDetail = trans.Details[1];
                    txtTransFactorL1.Text = transDetail.QuantityFactor.ToString();
                    txtTransItemL1.Text = transDetail.ItemID;
                    txtTransQuantityL1.Text = transDetail.Quantity.ToString();
                    if( transDetail.TaxList.Count> 0) 
                        txtTransTaxRateL1.Text = transDetail.TaxList[1].TaxRate.ToString();
                    if (trans.TransactionTaxIncluded)
                        txtTransUnitPriceL1.Text = transDetail.TaxIncludedPrice.ToString();
                    else
                        txtTransUnitPriceL1.Text = transDetail.UnitPrice.ToString();
                    txtTransUnL1.Text = transDetail.UnitOfSaleID;
                    txtTransWarehouseL1.Text = transDetail.WarehouseID.ToString();
                    // Lote
                    if (! string.IsNullOrEmpty(transDetail.Lot.LotID ) ) {
                        txtTransLotExpDateL1.Text = transDetail.Lot.ExpirationDate.ToShortDateString();
                        txtTransLotIdL1.Text = transDetail.Lot.LotID;
                        txtTransLotEditionL1.Text = transDetail.Lot.EditionID.ToString();
                        txtTransLotRetWeekL1.Text = transDetail.Lot.ReturnWeek.ToString();
                        txtTransLotRetYearL1.Text = transDetail.Lot.ReturnYear.ToString();
                        chkTransModuleLot.Checked = true;
                    }
                    // Cores e Tamanhos - Só na linha 1 
                    if (transDetail.Color.ColorID > 0) {
                        txtTransColor1.Text = transDetail.Color.ColorID.ToString();
                        chkTransModuleSizeColor.Checked = true;
                    }
                    if (transDetail.Size.SizeID > 0) {
                        txtTransSize1.Text = transDetail.Size.SizeID.ToString();
                        chkTransModuleSizeColor.Checked = true;
                    }
                    // Propriedades: Números de série
                    if (transDetail.ItemProperties.HasPropertyValues) {
                        lblTransPropNameL1.Text = transDetail.ItemProperties.PropertyID1;
                        txtTransPropValueL1.Text = transDetail.ItemProperties.PropertyValue1;
                        // Também é possivel utilizar as restantes 3 propriedades. Para isso necessitariamos de outra forma de apresentar os dados (com mais controlos, p.ex.)
                        //lblTransPropNameL1_2.Text = transDetail.ItemProperties.PropertyID2;
                        //txtTransPropValueL1_2.Text = transDetail.ItemProperties.PropertyValue2;
                        //lblTransPropNameL1_3.Text = transDetail.ItemProperties.PropertyID3;
                        //txtTransPropValueL1_3.Text = transDetail.ItemProperties.PropertyValue3;

                        chkTransModuleProps.Checked  = true;
                    }

                    // Linha 2 - Não tem cores e tamanhos 
                    if (trans.Details.Count > 1) {
                        transDetail = trans.Details[2];
                        txtTransFactorL2.Text = transDetail.QuantityFactor.ToString();
                        txtTransItemL2.Text = transDetail.ItemID;
                        txtTransQuantityL2.Text = transDetail.Quantity.ToString();
                        if(transDetail.TaxList.Count>0)
                            txtTransTaxRateL2.Text = transDetail.TaxList[1].TaxRate.ToString();
                        if (trans.TransactionTaxIncluded)
                            txtTransUnitPriceL2.Text = transDetail.TaxIncludedPrice.ToString();
                        else
                            txtTransUnitPriceL2.Text = transDetail.UnitPrice.ToString();
                        txtTransUnL2.Text = transDetail.UnitOfSaleID;
                        txtTransWarehouseL2.Text = transDetail.WarehouseID.ToString();
                        // Lote
                        if ( ! string.IsNullOrEmpty(transDetail.Lot.LotID) ) {
                            txtTransLotExpDateL2.Text = transDetail.Lot.ExpirationDate.ToShortDateString();
                            txtTransLotIdL2.Text = transDetail.Lot.LotID;
                            txtTransLotEditionL2.Text = transDetail.Lot.EditionID.ToString();
                            txtTransLotRetWeekL2.Text = transDetail.Lot.ReturnWeek.ToString();
                            txtTransLotRetYearL2.Text = transDetail.Lot.ReturnYear.ToString();

                            chkTransModuleLot.Checked = true;
                        }
                        // Propriedades: Números de série
                        if (transDetail.ItemProperties.HasPropertyValues) {
                            lblTransPropNameL2.Text = transDetail.ItemProperties.PropertyID1;
                            txtTransPropValueL2.Text = transDetail.ItemProperties.PropertyValue1;

                            chkTransModuleProps.Checked = true;
                        }
                    }
                }
                //
                // O Documento está anulado ?
                if ((int)trans.TransStatus == (int)TransStatusEnum.stVoid) {
                    tabBuySaleTransaction.BackgroundImage = Properties.Resources.stamp_Void;
                }
                else {
                    tabBuySaleTransaction.BackgroundImage = null;
                }
            }
            else {
                MessageBox.Show("A transação indicada não existe.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }


        private void TransactionClear() {
            chkTransModuleLot.Checked = false;
            chkTransModuleProps.Checked = false;
            chkTransModuleSizeColor.Checked = false;

            PartyTypeEnum  partyType = cmbTransPartyType.SelectedIndex == 0 ? PartyTypeEnum.ptSupplier : PartyTypeEnum.ptCustomer;
            txtTransColor1.Text = string.Empty;
            txtTransCurrency.Text = systemSettings.BaseCurrency.CurrencyID;
            txtTransDate.Text = DateTime.Today.ToShortDateString();
            string docId = string.Empty;

            if (rbTransBuySell.Checked) {
                if (partyType == PartyTypeEnum.ptCustomer) {
                    docId = systemSettings.WorkstationInfo.DefaultTransDocument;
                    cmbTransPartyType.SelectedIndex = 1;
                }
                else {
                    cmbTransPartyType.SelectedIndex = 0;
                    var docs = systemSettings.WorkstationInfo.Document.FindByNature(TransactionNatureEnum.Purchase_YourInvoice);
                    if (docs.Count > 0) {
                        docId = docs.get_ItemByIndex(1).DocumentID;
                    }
                }
                if (!string.IsNullOrEmpty(docId)) {
                    var doc = systemSettings.WorkstationInfo.Document[docId];
                    chkTransTaxIncluded.Checked = doc.TaxIncludedPrice;
                }
            }
            else {
                var docs = systemSettings.WorkstationInfo.Document.FindByNature(TransactionNatureEnum.Stock_Release);
                if (docs.Count > 0) {
                    var doc = docs.get_ItemByIndex(1);
                    docId = doc.DocumentID;
                    chkTransTaxIncluded.Checked = doc.TaxIncludedPrice;
                }
                // partyTye = Nenhum
                cmbTransPartyType.SelectedIndex = 2;
            }
            //
            txtTransDoc.Text = docId;
            txtTransDocNumber.Text = "0";
            txtTransGlobalDiscount.Text = "0";
            txtTransPartyId.Text = string.Empty;
            txtPaymentID.Text = "0";
            txtTenderID.Text = "0";
            //
            // Obter a primeira série EXTERNA
            var externalSeries = systemSettings.DocumentSeries
                                               .OfType<DocumentsSeries>()
                                               .FirstOrDefault(x => x.SeriesType == SeriesTypeEnum.SeriesExternal);
            if (externalSeries != null)
                txtTransSerial.Text = externalSeries.Series;
            //
            TransClearL1();
            TransClearL2();
            //
            tabBuySaleTransaction.BackgroundImage = null;
        }

        private void TransClearL1() {
            txtTransItemL1.Text = string.Empty;
            txtTransFactorL1.Text = "1";
            txtTransQuantityL1.Text = string.Empty;
            txtTransTaxRateL1.Text = string.Empty;
            txtTransUnitPriceL1.Text = string.Empty;
            txtTransUnL1.Text = systemSettings.SystemInfo.ItemDefaultUnit;
            txtTransWarehouseL1.Text = string.Empty;

            //Propriedades: NS
            TransClearNS1();

            // Lotes (edições)
            TransClearLotL1();

            //Size and colors
            TransClearSize1();
            TransClearColor1();

        }

        private void TransClearL2() {
            txtTransFactorL2.Text = "1";
            txtTransItemL2.Text = string.Empty;
            txtTransQuantityL2.Text = string.Empty;
            txtTransUnL2.Text = systemSettings.SystemInfo.ItemDefaultUnit;
            txtTransTaxRateL2.Text = string.Empty;
            txtTransUnitPriceL2.Text = string.Empty;
            txtTransWarehouseL2.Text = string.Empty;

            //Propriedades: NS
            TransClearNS2();

            // Lotes (edições)
            TransClearLotL2();
        }

        private void TransClearLotL1() {
            txtTransLotExpDateL1.Text = string.Empty;
            txtTransLotIdL1.Text = string.Empty;
            txtTransLotEditionL1.Text = string.Empty;
            var currentDate = DateTime.Today;
            txtTransLotRetWeekL1.Text = GetWeekOfYear( currentDate ).ToString();
            txtTransLotRetYearL1.Text = currentDate.Year.ToString();
        }
        private void TransClearLotL2() {
            txtTransLotExpDateL2.Text = string.Empty;
            txtTransLotIdL2.Text = string.Empty;
            txtTransLotEditionL2.Text = string.Empty;
            var currentDate = DateTime.Today;
            txtTransLotRetWeekL2.Text = GetWeekOfYear(currentDate).ToString();
            txtTransLotRetYearL2.Text = currentDate.Year.ToString();
        }
        private void TransClearNS1() {
            txtTransPropValueL1.Text = string.Empty;
        }
        private void TransClearNS2() {
            txtTransPropValueL2.Text = string.Empty;
        }
        private void TransClearSize1() {
            txtTransSize1.Text = string.Empty;
        }
        private void TransClearColor1() {
            txtTransColor1.Text = string.Empty;
        }

        private void btnTransClearL1_Click(object sender, EventArgs e) {
            TransClearL1();
        }

        private void btnTransClearLTL1_Click(object sender, EventArgs e) {
            TransClearLotL1();
        }

        private void btnTransClearLTL2_Click(object sender, EventArgs e) {
            TransClearLotL2();
        }

        private void btnTransClearNSL1_Click(object sender, EventArgs e) {
            TransClearNS1();
        }

        private void btnTransClearNSL2_Click(object sender, EventArgs e) {
            TransClearNS2();
        }

        private void btnTransClearSz_Click(object sender, EventArgs e) {
            TransClearSize1();
        }

        private void btnTransClearColor1_Click(object sender, EventArgs e) {
            TransClearColor1();
        }

        private void btnTransClearL2_Click(object sender, EventArgs e) {
            TransClearL2();
        }

        private short GetWeekOfYear(DateTime currentDate) {
            short lotRetWeek = (short)System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(currentDate,
                                                                                                             System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.CalendarWeekRule,
                                                                                                            System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek);
            if (lotRetWeek < 52) lotRetWeek++;
            return lotRetWeek;
        }
        
        #endregion


        #region Stock Transaction

        private void TransStockAddDetail( short warehouseId, string itemId, string unitOfSaleId, double itemTaxRate, double Quantity, double unitPrice ){
            StockTransaction stockTrans = bsoStockTransaction.Transaction;
            
            double lngLineItemID = stockTrans.Details.Count+1;
    
            bool blnCanAddDetail = true;
            var transDetail = new ItemTransactionDetail();
            transDetail.BaseCurrency = stockTrans.BaseCurrency;
            transDetail.CreateDate = stockTrans.CreateDate;
            transDetail.ActualDeliveryDate = stockTrans.ActualDeliveryDate;
            transDetail.PartyTypeCode = stockTrans.PartyTypeCode;
            transDetail.PartyID = stockTrans.PartyID;
            //
            //*** WAREHOUSE
            if(warehouseId > 0 )
                if(dsoCache.WarehouseProvider.WarehouseExists(warehouseId) )
                    transDetail.WarehouseID = warehouseId;
                else
                    transDetail.WarehouseID = warehouseId;
            else
                transDetail.WarehouseID = warehouseId;
            //
            transDetail.WarehouseOutgoing = transDetail.WarehouseID;
            transDetail.WarehouseReceipt = transDetail.WarehouseID;
    
            ////***STOCK TRANSFER ONLY -- uncomment to set
            //if (systemSettings.WorkstationInfo.Document[stockTrans.TransDocument].StockBehavior == StockBehaviorEnum.sbStockTransfer)
            //    transDetail.WarehouseReceipt = bsoStockTrans.WarehouseReceipt;
            
            ////    *** DESTINATION WAREHOUSE -- uncomment to set
            //short warehouseReceiptId=2;
            //if( dsoCache.WarehouseProvider.WarehouseExists(warehouseReceiptId) ){
            //    transDetail.WarehouseReceipt = warehouseReceiptId;
        
            //    if( transDetail.ComponentList !=null ){
            //        foreach( ItemTransactionDetail transDetailSave in transDetail.ComponentList )
            //            transDetailSave.WarehouseID = warehouseReceiptId;
            //    }
            //}
    
            ////*** SOURCE WAREHOUSE -- uncomment to set
            //short warehouseOutgoingId = 1;
            //if( dsoCache.WarehouseProvider.WarehouseExists(warehouseOutgoingId) ){
            //    transDetail.WarehouseOutgoing = warehouseOutgoingId;
        
            //    if( systemSettings.WorkstationInfo.Document[stockTrans.TransDocument].StockBehavior == StockBehaviorEnum.sbStockTransfer ){
            //        if( warehouseOutgoingId != transDetail.WarehouseID )
            //            transDetail.WarehouseID = warehouseReceiptId;
            //    }
        
            //    if( transDetail.ComponentList!=null){
            //        foreach( ItemTransactionDetail transDetailSave in transDetail.ComponentList )
            //            transDetailSave.WarehouseID = warehouseOutgoingId;
            //    }
            //}

            //LineItemId
            transDetail.LineItemID = lngLineItemID;
            //
            //-----> INFORMAÇÕES DO PRODUTO
            var item = dsoCache.ItemProvider.GetItemForTransactionDetail(itemId, transDetail.BaseCurrency);

            if( item != null ){
                transDetail.ItemID = item.ItemID;
                transDetail.Description = item.Description;
                transDetail.TaxableGroupID = item.TaxableGroupID;
                transDetail.ItemType = item.ItemType;
                transDetail.FamilyID = item.Family.FamilyID;
                transDetail.UnitList = item.UnitList.Clone();
        
                transDetail.WeightUnitOfMeasure = item.WeightUnitOfMeasure;
                transDetail.WeightMeasure = item.WeightMeasure;
                transDetail.Graduation = item.Graduation;
                transDetail.ItemTax = item.ItemTax;
                transDetail.ItemTax2 = item.ItemTax2;
                transDetail.ItemTax3 = item.ItemTax3;
                transDetail.ItemExtraInfo.ItemQuantityCalcFormula = item.ItemQuantityCalcFormula;
        
                if( item.UnitList.IsInCollection(unitOfSaleId) )
                    transDetail.UnitOfSaleID = unitOfSaleId;
                else
                    transDetail.UnitOfSaleID = item.GetDefaultUnitForTransaction(DocumentTypeEnum.dcTypeStock);

               //*** PROPERTIES -- Uncomment to use
                //if(item.PropertyEnabled){
                //    transDetail.ItemProperties.PropertyID1 = item.PropertyID1;
                //    transDetail.ItemProperties.PropertyID2 = item.PropertyID2;
                //    transDetail.ItemProperties.PropertyID3 = item.PropertyID3;
                //    transDetail.ItemProperties.UsePriceOnProp1 = item.UsePriceOnProp1;
                //    transDetail.ItemProperties.UsePriceOnProp2 = item.UsePriceOnProp2;
                //    transDetail.ItemProperties.UsePriceOnProp3 = item.UsePriceOnProp3;
                //    transDetail.ItemProperties.ControlType = item.PropertyControlType;
                //    transDetail.ItemProperties.ControlMode = item.PropertyControlMode;
                //    transDetail.ItemProperties.UseExpirationDate = item.PropertyUseExpirationDate;
                //    transDetail.ItemProperties.UseProductionDate = item.PropertyUseProductionDate;
                //    transDetail.ItemProperties.ExpirationDateControl = item.PropertyExpirationDateControl;
                //    transDetail.ItemProperties.MaximumQuantity = item.PropertyMaximumQuantity;
                //    transDetail.ItemProperties.ResetValues();
        
                //    transDetail.ItemProperties.PropertyValue1 = ... value 1
                //    transDetail.ItemProperties.PropertyValue1_Key2 = ... key 2
                //    transDetail.ItemProperties.PropertyValue1_Key3 = ... key 3
                //    transDetail.ItemProperties.PropertyValue2 = ... value 2
                //    transDetail.ItemProperties.PropertyValue2_Key2 = ... key 2
                //    transDetail.ItemProperties.PropertyValue2_Key3 = ... key 3
                //    transDetail.ItemProperties.PropertyValue3 = ... value 3
                //    transDetail.ItemProperties.PropertyValue3_Key2 = ... key 2
                //    transDetail.ItemProperties.PropertyValue3_Key3 = ... key 3
                //}
            }
            else if (itemId == "=") {
                //*** COMMENT LINE
                item = new Item();
                //ItemId: //=// represents comment line
                item.ItemID = "=";
                item.Description = "Só descrição";
                item.ItemType = ItemTypeEnum.itmComments;
                item.UnitOfSaleID = systemSettings.SystemInfo.ItemDefaultUnit;
                item.AlternativeUnitOfStock = systemSettings.SystemInfo.ItemDefaultUnit;
                item.DefaultStockUnit = systemSettings.SystemInfo.ItemDefaultUnit;
                item.DefaultBuyUnit = systemSettings.SystemInfo.ItemDefaultUnit;
                item.DefaultSellingUnit = systemSettings.SystemInfo.ItemDefaultUnit;
                item.TaxableGroupID = systemSettings.SystemInfo.DefaultTaxableGroupID;
                item.CurrencyID = systemSettings.BaseCurrency.CurrencyID;
                item.CurrencyExchange = systemSettings.BaseCurrency.SaleExchange;
                item.CurrencyFactor = systemSettings.BaseCurrency.EuroConversionRate;
            }
            else {
                throw new Exception(string.Format("O Artigo [{0}] não foi entrado.", itemId));
            }

            //-----> Taxa de IVA
            transDetail.TaxableGroupID = dsoCache.TaxesProvider.GetTaxableGroupIDFromTaxRate(itemTaxRate, systemSettings.SystemInfo.DefaultCountryID, systemSettings.SystemInfo.TaxRegionID);

            //-----> Cores e Tamanhos. Uncomment to SET
            //short ColorId = 3;
            //short SizeId = 4;
            //if( item != null ){
            //    if( item.Colors.Count > 0 && item.Sizes.Count > 0 ){
            //        var color = dsoCache.ColorProvider.GetColor(ColorId);
            //        if( color !=null )
            //            transDetail.Color = color;
        
            //        var size = dsoCache.SizeProvider.GetSize(SizeId);
            //        if( size != null )
            //            transDetail.Size = size;
        
            //        if( transDetail.Color.ColorID == 0 ){
            //            foreach( ItemColor itemColor in item.Colors ){
            //                color = dsoCache.ColorProvider.GetColor(itemColor.ColorID);
            //                if( color !=null )
            //                    transDetail.Color = color;
            //                break;
            //            }
            //        }
        
            //        if( transDetail.Size.SizeID == 0){
            //            foreach( ItemSize itemSize in item.Sizes ){
            //                size = dsoCache.SizeProvider.GetSize(itemSize.SizeId);
            //                if( size !=null )
            //                    transDetail.Size = size;
            //                break;
            //            }
            //        }
            //    }
            //}
            //
            transDetail.SetUnitOfSaleID(transDetail.UnitOfSaleID);
            
            //Formulas
            double Quantity1=0;
            double Quantity2=0;
            double Quantity3=0;
            double Quantity4=0;

            ////*** Packs -- Uncomment to set
            //double packQuantity=10;
            //if( transDetail.UnitConversion != 0 && packQuantity != 0)
            //    transDetail.PackQuantity = packQuantity;
   
            bool blnHaveSetUnits = false;
            ////*** Units -- uncomment to set
            //double units = 10;
            //if( units != 0){
            //    transDetail.SetUnits(units);
            //    blnHaveSetUnits = true;
            //}
    
            transDetail.Quantity1 = Quantity1;
            transDetail.Quantity2 = Quantity2;
            transDetail.Quantity3 = Quantity3;
            transDetail.Quantity4 = Quantity4;    
            if( !blnHaveSetUnits ){
                if( ! string.IsNullOrEmpty(transDetail.ItemExtraInfo.ItemQuantityCalcFormula) && systemSettings.SystemInfo.UseUnitWithFormulaItems )
                    transDetail.SetQuantity( CalculateQuantity(transDetail.ItemExtraInfo.ItemQuantityCalcFormula, transDetail, true) );
                else
                    transDetail.SetQuantity( CalculateQuantity(null, transDetail, true));
            }
            //    
            if( ! blnHaveSetUnits )
                transDetail.SetQuantity(Quantity);
            transDetail.Description = item.Description;     // OR "Custom description"
            transDetail.Comments = "Observações de linha: Gerada por" + Application.ProductName; 
    
            //*** UnitPrice
            if( bsoStockTransaction.TransactionTaxIncluded )
                transDetail.TaxIncludedPrice = unitPrice;
            else
                transDetail.UnitPrice = unitPrice;
            //
            // Descomentar para indicar desconto na linha
            //transDetail.DiscountPercent = 10;
            //
            ////Desconto cumulativo - Descomentar para indicar
            //transDetail.CumulativeDiscountPercent1 = 1
            //transDetail.CumulativeDiscountPercent2 = 2
            //transDetail.CumulativeDiscountPercent3 = 3

            RTLUtil16.MathFunctions mathUtil = new MathFunctions();

            if(transDetail.DiscountPercent == 0 && (transDetail.CumulativeDiscountPercent1 != 0 || transDetail.CumulativeDiscountPercent2 != 0 || transDetail.CumulativeDiscountPercent3 != 0))
                transDetail.DiscountPercent = mathUtil.GetCumulativeDiscount(transDetail.CumulativeDiscountPercent1, transDetail.CumulativeDiscountPercent2, transDetail.CumulativeDiscountPercent3);
    
            if( transDetail.DiscountPercent != 0 && (transDetail.CumulativeDiscountPercent1 == 0 && transDetail.CumulativeDiscountPercent2 == 0 && transDetail.CumulativeDiscountPercent3 == 0) )
                transDetail.CumulativeDiscountPercent1 = transDetail.DiscountPercent;

            ////*** Kit ITEMS -- Uncomment to use
            //if( item != null ){
            //    if( item.ItemType == ItemTypeEnum.itmKit){
            //        transDetail.ComponentList = bsoStockTrans.BSOCommonTransaction.GetComponentList(transDetail, item.ItemCollection, transDetail.Quantity, item.NeededComponents, item.UseComponentPrices, "PCUP", 0);
            //    }
            //}
            //transDetail.ItemExtraInfo.DoNotGroup = true;

            //*** PROPERTIES
            if( transDetail.ItemProperties.HasPropertyValues )
                dsoCache.ItemPropertyProvider.GetItemPropertyStock( transDetail.ItemID, transDetail.WarehouseID, transDetail.ItemProperties);
    
            //*** Delivery time -- Uncomment to set
            //transDetail.RequiredDeliveryDateTime = DateTime.Now.AddDays(10);  // Hoje + 10 dias

            if( blnCanAddDetail ){
                bool calculate = true;
                bsoStockTransaction.AddDetail( transDetail, ref calculate );
            }
            item= null;
        }


        private double CalculateQuantity(string strFormula, ItemTransactionDetail TransactionDetail, bool UseQuantityFactor){
            MathFunctions mathUtil = new MathFunctions();
            UnitOfMeasure oUnit;
            BSOExpressionParser objBSOExpressionParser= new BSOExpressionParser();
            double result=0;

            if (!string.IsNullOrEmpty(strFormula)) {
                result = 0;
                string tempres = objBSOExpressionParser.ParseFormula(strFormula, TransactionDetail);
                double.TryParse(tempres, out result);
            }
            else
                result = TransactionDetail.Quantity;
    
            if( UseQuantityFactor ){
                if( TransactionDetail.QuantityFactor != 1 && TransactionDetail.QuantityFactor != 0 )
                    result = result / TransactionDetail.QuantityFactor;
            }
    
            oUnit = dsoCache.UnitOfMeasureProvider.GetUnitOfMeasure(TransactionDetail.UnitOfSaleID);
            if (oUnit != null) {
                result = mathUtil.MyRoundEx(result, oUnit.MaximumDecimals);
            }
            oUnit = null;
    
            objBSOExpressionParser = null;
            mathUtil = null;

            return result;
        }



        private TransactionID TransactionStockUpdate(string transSerial, string transDocument, double transDocNumber, bool isNew) {
            bool blnSaved = false;
            TransactionID resultTransId = null;

            if (! systemSettings.WorkstationInfo.Document.IsInCollection(transDocument)) {
                throw new Exception( string.Format("O documento [{0}] não existe ou não se encontra preenchido.", transDocument) );
            }

            DocumentsSeries transSeries = null;
            if( systemSettings.DocumentSeries.IsInCollection( transSerial) ){
                transSeries = systemSettings.DocumentSeries[ transSerial ];
                if( transSeries.SeriesType != SeriesTypeEnum.SeriesExternal ){
                    throw new Exception("Apenas são permitidas séries externas.");
                }
            }
            if( transSeries == null ){
                throw new Exception("A série indicada não existe");
            }
            //
            var transType = TransGetType();
            if ( transType != DocumentTypeEnum.dcTypeStock ) {
                throw new Exception(string.Format("O documento indicado [{0}] não é um documento de stock", transDocument));
            }

            var objDSOStockTransaction = new DSOStockTransaction();

            //var DocTransStatus = TransStatusEnum.stNormal;
            blnSaved = false;

            bsoStockTransaction.PermissionsType = FrontOfficePermissionEnum.foPermByUser;
            if (isNew) {
                bsoStockTransaction.InitNewTransaction(transDocument, transSerial);
                if(transDocNumber > 0)
                    bsoStockTransaction.Transaction.TransDocNumber = transDocNumber;
            }
            else {
                var loadResult = bsoStockTransaction.LoadStockTransaction(transType, transSerial, transDocument, transDocNumber);
                if (!loadResult) {
                    throw new Exception(string.Format("Não foi possivel carregar o documento {0} {1}/{2}.", transDocument, transSerial, transDocNumber));
                }
            }
            var bsoCommonTransaction = bsoStockTransaction.BSOCommonTransaction;
    
            //Taxes included?
            bool transTaxIncluded = chkTransTaxIncluded.Checked;
            bsoStockTransaction.TransactionTaxIncluded = transTaxIncluded;
            bsoCommonTransaction.TransactionTaxIncluded = transTaxIncluded;
            //
            bsoCommonTransaction.TransactionType = DocumentTypeEnum.dcTypeStock;
            bsoStockTransaction.Transaction.TransDocType = DocumentTypeEnum.dcTypeStock;
            //
            DateTime createDate = DateTime.Today;
            DateTime.TryParse( txtTransDate.Text, out createDate );
            bsoStockTransaction.createDate = createDate;
            //bsoStockTransaction.CheckCreateDate = createDate;
            bsoStockTransaction.CreateTime = new DateTime( DateTime.Now.TimeOfDay.Ticks );
            bsoStockTransaction.ActualDeliveryDate = createDate;
            //
            // Descomentar a linha seguiinte para indicar uma referência livre
            //bsoStockTransaction.ContractReferenceNumber = "External REF"
        
            //Party RELATED INFO (can be ignored)
            PartyTypeEnum partyType = TransGetPartyType();
            bsoStockTransaction.PartyType = (short)partyType;
            double partyId = 0;
            double.TryParse( txtTransPartyId.Text, out partyId );
            if( bsoStockTransaction.CheckPartyID(partyId) ){
                bsoStockTransaction.PartyID = partyId;
            }
            //TODO: Verify
            bsoCommonTransaction.CountryID = systemSettings.SystemInfo.DefaultCountryID;
            bsoCommonTransaction.TaxRegionID = systemSettings.SystemInfo.TaxRegionID;
            bsoCommonTransaction.EntityFiscalStatusID = bsoStockTransaction.Transaction.PartyFiscalStatus;
            //------------> ZONA
            //------------> MOEDA
            var currency = dsoCache.CurrencyProvider.GetCurrency(txtTransCurrency.Text);
            if (currency == null) currency = systemSettings.BaseCurrency;
            bsoStockTransaction.BaseCurrency = currency.CurrencyID;
            bsoStockTransaction.BaseCurrencyExchange = currency.BuyExchange;
        
            // Observações
            // Modificar para acrescentar ou retirar observações livres
            bsoStockTransaction.Transaction.Comments = "Gerado por: " + Application.ProductName;
    
            var transStock = bsoStockTransaction.Transaction;

            //-------------------------------------------------------------
            // *** DETALHES
            //-------------------------------------------------------------
            // Remover todas as linhas (caso da alteração)
            int i=1;
            while (transStock.Details.Count > 0) {
                transStock.Details.Remove(ref i);
            }

            //
            //Linha 1
            string itemId = txtTransItemL1.Text;
            short wareHouseId = 0;
            short.TryParse(txtTransWarehouseL1.Text, out wareHouseId);
            string unitOfMovId = txtTransUnL1.Text;
            double taxRate = 0;
            double.TryParse(txtTransTaxRateL1.Text, out taxRate);
            double qty = 0;
            double.TryParse(txtTransQuantityL1.Text, out qty);
            double unitPrice = 0;
            double.TryParse(txtTransUnitPriceL1.Text, out unitPrice);
            if (!string.IsNullOrEmpty(itemId)) {
                TransStockAddDetail(wareHouseId, itemId, unitOfMovId, taxRate, qty, unitPrice);
            }
            //
            // Linha 2
            itemId = txtTransItemL2.Text.Trim();
            if (!string.IsNullOrEmpty(itemId)) {
                wareHouseId = 0;
                short.TryParse(txtTransWarehouseL2.Text, out wareHouseId);
                unitOfMovId = txtTransUnL2.Text;
                taxRate = 0;
                double.TryParse(txtTransTaxRateL2.Text, out taxRate);
                qty = 0;
                double.TryParse(txtTransQuantityL2.Text, out qty);
                unitPrice = 0;
                double.TryParse(txtTransUnitPriceL2.Text, out unitPrice);
                TransStockAddDetail(wareHouseId, itemId, unitOfMovId, taxRate, qty, unitPrice);
            }
            //
            if (bsoStockTransaction.Transaction.Details.Count == 0) {
                throw new Exception("O documento não tem linhas.");
            }
            //
            //*** SAVE
            if(! blnSaved ){
                if (bsoStockTransaction.Transaction.Details.Count > 0) {
                    // Colocar a false para não imprimir.
                    // A Impressão não é atualmente suportada em .NET
                    bool printDoc = false;

                    bsoStockTransaction.SaveDocumentEx( true, ref printDoc);

                    resultTransId = new TransactionID();
                    resultTransId.TransSerial = transStock.TransSerial;
                    resultTransId.TransDocument = transStock.TransDocument;
                    resultTransId.TransDocNumber = transStock.TransDocNumber;
                }
                else {
                    throw new Exception("O documento não tem linhas");
                }

                ////Documento anulado -- Descomentar
                //if (DocTransStatus == TransStatusEnum.stVoid) {
                //    if (bsoStockTransaction.LoadStockTransaction(DocumentTypeEnum.dcTypeStock, transSerial, transDocument, transDocNumber)) {
                //        objDSOStockTransaction.Delete(transStock);
                //        blnSaved = true;
                //    }
                //    else
                //        bsoStockTransaction.Transaction.TransStatus = TransStatusEnum.stVoid;
                //}
            }
            bsoCommonTransaction = null;
            objDSOStockTransaction = null;

            return resultTransId;
        }


        #endregion


        private void btnClear_Click(object sender, EventArgs e) {
            switch (tabEntities.SelectedIndex) {
                case 0: ItemClear( false ); break;
                case 1: CustomerClear(); break;
                case 2: SupplierClear(); break;
                case 3: TransactionClear(); break;
                case 4: AccountTransactionClear(); break;
                
                case 5: UnitOfMeasureClear(); break;
            }
        }

        private void chkTransModuleProps_CheckedChanged(object sender, EventArgs e) {
            pnlTransModuleProp.Enabled = chkTransModuleProps.Checked;
        }

        private void chkTransModuleLot_CheckedChanged(object sender, EventArgs e) {
            pnlTransModuleLot.Enabled = chkTransModuleLot.Checked;
        }

        private void chkTransModuleSizeColor_CheckedChanged(object sender, EventArgs e) {
            pnlTransModuleSizeColor.Enabled = chkTransModuleSizeColor.Checked;
        }

        private void tabEntities_SelectedIndexChanged(object sender, EventArgs e) {
        }

        private void chkTransModuleLot_CheckedChanged_1(object sender, EventArgs e) {
            pnlTransModuleLot.Enabled = chkTransModuleLot.Checked;
        }


        #region Account documents

        private TransactionID AccountTransactionRemove() {
            TransactionID transId = null;
            string transSerial = txtAccountTransSerial.Text;
            string transDoc = txtAccountTransDoc.Text;
            double transDocNumber = 0;
            double.TryParse(txtAccountTransDocNumber.Text, out transDocNumber);
            //
            // Obter a transação (recibo ou pagamento)
            var result = accountTransManager.LoadTransaction(transSerial, transDoc, transDocNumber);
            if( !result )
                throw new Exception( string.Format(" O documento {0} {1}/{2} não existe ou não é possivel carregá-lo.", transDoc, transSerial, transDocNumber ) );
            //
            //Colocar o motivo de isenção: obrigatóriedade depende da definição do documento
            accountTransManager.Transaction.VoidMotive = "Anulado por " + Application.ProductName;
            // Anular o documento
            if (accountTransManager.DeleteDocument())
                transId = accountTransManager.Transaction.TransactionID;
            else
                throw new Exception(string.Format("Não foi possivel anular o documento {0} {1}/{2}.", transDoc, transSerial, transDocNumber));

            return transId;
        }



        private void AccountTransAddDetail( AccountTransactionManager accountTransMan, AccountUsedEnum accountUsed, string accountTypeId,
                                            string docId, string docSeries, double docNumber, short transInstallment,  double paymentValue) {
            // Linhas
            if (paymentValue > 0) {
                AccountTransaction accountTrans = accountTransMan.Transaction;

                if (systemSettings.WorkstationInfo.Document.IsInCollection(docId)) {
                    // Obter o pendente. PAra efeito de exemplo consideramos que não há prestações (installmentId=0)
                    var ledger = accountTransMan.LedgerAccounts.OfType<LedgerAccount>().FirstOrDefault(x => x.TransDocument == docId && x.TransSerial == docSeries && x.TransDocNumber == docNumber && x.TransInstallmentID == transInstallment);
                    if (ledger != null) {
                        if (paymentValue > ledger.TotalPendingAmount ) {
                            throw new Exception(string.Format("O valor a pagar é superior ao valor em divida no documento: {0} {1}/{2}", docId, docSeries, docNumber));
                        }
                        AccountTransactionDetail detail = accountTrans.Details.Find(docSeries, docId, docNumber, transInstallment);
                        if( detail == null )
                            detail = new AccountTransactionDetail();
                        // Lançar o pagamento correcto, acertando também a retenção.
                        accountTransMan.SetPaymentValue(ledger.Guid, paymentValue);
                        //
                        // Copiar o pendente para o pagamento
                        detail.AccountTypeID = ledger.PartyAccountTypeID;
                        detail.BaseCurrency = accountTrans.BaseCurrency;
                        detail.DocContractReference = ledger.ContractReferenceNumber;
                        detail.DocCreateDate = ledger.CreateDate;
                        detail.DocCurrency = ledger.BaseCurrency;
                        detail.DocDeferredPaymentDate = ledger.DeferredPaymentDate;
                        detail.DocID = ledger.TransDocument;
                        detail.DocInstallmentID = ledger.TransInstallmentID;
                        detail.DocNumber = ledger.TransDocNumber;
                        detail.DocSerial = ledger.TransSerial;
                        //detail.ExchangeDifference = 
                        detail.LedgerGUID = ledger.Guid;
                        detail.PartyID = accountTrans.Entity.PartyID;
                        detail.PartyTypeCode = (short)accountTrans.Entity.PartyType;
                        detail.RetentionOriginalAmount = ledger.RetentionTotalAmount;
                        detail.RetentionPayedAmount = ledger.RetentionPayedAmount;
                        detail.RetentionPendingAmount = ledger.RetentionPendingAmount - ledger.RetentionPayedAmount;
                        //detail.TaxValues
                        detail.TotalDiscountAmount = ledger.DiscountValue;
                        detail.TotalDiscountPercent = ledger.DiscountPercent;
                        detail.TotalOriginalAmount = ledger.TotalAmount;
                        detail.TotalPayedAmount = ledger.PaymentValue;
                        detail.TotalPendingAmount = ledger.TotalPendingAmount - ledger.PaymentValue;
                        //
                        detail.TransDocNumber = accountTrans.TransDocNumber;
                        detail.TransDocument = accountTrans.TransDocument;
                        detail.TransSerial = accountTrans.TransSerial;
                        //
                        detail.CashAccountingSchemeType = ledger.CashAccountingSchemeType;
                        //
                        accountTrans.Details.Add(ref detail);
                    }
                }
            }
        }


        /// <summary>
        /// Insere ou altera um pagamento ou recibo
        /// 
        /// </summary>
        /// <param name="newDoc"></param>
        private TransactionID AccountTransactionUpdate( bool newDoc ) {
            const string ACCOUNT_TYPE = "CC";                   //Como exemplo, sóvamos utilizar a carteira de contas correntes
            string transSerial = txtAccountTransSerial.Text.ToUpper();
            string transDoc = txtAccountTransDoc.Text.ToUpper();
            double transDocNumber = 0;
            double.TryParse( txtAccountTransDocNumber.Text, out transDocNumber);
            double partyId = 0;
            double.TryParse(txtAccountTransPartyId.Text, out partyId);
            TransactionID result = null;
            //
            AccountUsedEnum accountUsed = AccountUsedEnum.auNone;
            if (cmbRecPeg.SelectedIndex == 0) {
                accountUsed = AccountUsedEnum.auCustomerLedgerAccount;
                accountTransManager.InitManager(accountUsed);
            }
            else {
                accountUsed = AccountUsedEnum.auSupplierLedgerAccount;
                accountTransManager.InitManager( accountUsed );
            }
            if (newDoc) {
                accountTransManager.InitNewTransaction(transSerial, transDoc, transDocNumber);
                accountTransManager.SetPartyID(partyId);
            }
            else {
                accountTransManager.LoadTransaction(transSerial, transDoc, transDocNumber);
            }
            var accountTrans = accountTransManager.Transaction;
            if (accountTrans == null) {
                throw new Exception(string.Format("Não foi possivel iniciar/carregar o documento {0} {1}/{2}", transDoc, transSerial, transDocNumber));
            }
            //Obter a conta corrente do cliente
            accountTrans.LedgerAccounts = dsoCache.LedgerAccountProvider.GetLedgerAccountList(accountUsed, ACCOUNT_TYPE, partyId, accountTrans.BaseCurrency);
            if (accountTrans.LedgerAccounts.Count == 0) {
                throw new Exception(string.Format("A entidade [{0}] não tem pendentes na carteira [{1}].", partyId, ACCOUNT_TYPE));
            }
            //
            // Remover todos os pagamentos da ledger account (se o recibo estiver a ser alterado)
            int i = 1;
            while (accountTrans.Details.Count > 0) {
                accountTrans.Details.Remove(ref i);
            }
            accountTransManager.SetAccountID(ACCOUNT_TYPE); // Conta corrente
            accountTransManager.SetBaseCurrencyID(txtAccountTransDocCurrency.Text);
            DateTime createDate = DateTime.Today;
            DateTime.TryParse( txtAccountTransDocDate.Text, out createDate );
            accountTransManager.SetCreateDate(createDate);
            //
            // Linhas
            // Linha 1
            string docId = txtAccountTransDocL1.Text;
            string docSeries = txtAccountTransSeriesL1.Text;
            double docNumber = 0;
            double.TryParse(txtAccountTransDocNumberL1.Text, out docNumber);
            double paymentValue = 0;
            double.TryParse(txtAccountTransDocValueL1.Text, out paymentValue);
            if (paymentValue > 0) {
                AccountTransAddDetail(accountTransManager, accountUsed, ACCOUNT_TYPE, docId, docSeries, docNumber, 0, paymentValue);
            }
            // Linha 2
            docId = txtAccountTransDocL2.Text;
            docSeries = txtAccountTransSeriesL2.Text;
            docNumber = 0;
            double.TryParse(txtAccountTransDocNumberL2.Text, out docNumber);
            paymentValue = 0;
            double.TryParse(txtAccountTransDocValueL2.Text, out paymentValue);
            if (paymentValue > 0) {
                AccountTransAddDetail(accountTransManager, accountUsed, ACCOUNT_TYPE, docId, docSeries, docNumber, 0, paymentValue);
            }
            //
            // Não continuar se o documento não tiver linhas
            if (accountTrans.Details.Count == 0) {
                throw new Exception("O documento não tem linhas.");
            }
            //
            accountTrans.TenderLineItems = AccountTransGetTenderLineItems( accountTransManager );
            //
            // Gravar
            if (!accountTransManager.SaveDocumentEx(false)) {
                throw new Exception("A gravação do recibo falhou!");
            }
            else {
                result = accountTransManager.Transaction.TransactionID;
            }

            return result;
        }


        /// <summary>
        /// Preencher os meios de pagamentos utilizados no recebimento/pagamento
        /// </summary>
        TenderLineItemList AccountTransGetTenderLineItems(AccountTransactionManager accountTransManager) {
            //
            var accountTrans = accountTransManager.Transaction;
            var TenderLines = new TenderLineItemList();

            // Tender -- modo(s) de pagamento(s)
            short tenderId = dsoCache.TenderProvider.GetFirstTenderCash();
            short.TryParse(txtAccountTransPaymentId.Text, out tenderId);
            var tender = dsoCache.TenderProvider.GetTender(tenderId);
            // Add tender line
            var tenderLine = new TenderLineItem();
            tenderLine.Tender = tender;
            tenderLine.Amount = accountTrans.TotalAmount;
            // Caixa. dever ser a caixa aberta do sistema. Para simplificar colocou-se a default do sistema
            tenderLine.TillId = systemSettings.WorkstationInfo.DefaultMainTillID;
            tenderLine.TenderCurrency = accountTrans.BaseCurrency;
            tenderLine.PartyTypeCode = accountTrans.PartyTypeCode;
            tenderLine.PartyID = accountTrans.Entity.PartyID;
            tenderLine.CreateDate = DateTime.Today;
            //
            // Por uma questão de simplificação, neste exemplo apenas se vai considerar um pagamento de um só cheque.
            if (tender.TenderType == TenderTypeEnum.tndCheck) {
                TenderCheck tenderCheck = null;
                if( tenderLine.TenderCheck == null ){
                    tenderLine.TenderCheck = new TenderCheck();
                }
                tenderCheck = tenderLine.TenderCheck;
                tenderCheck.CheckAmount = tenderLine.Amount;
                tenderCheck.CheckDeferredDate = tenderLine.CreateDate;
                tenderCheck.TillId = tenderLine.TillId;
                    
                tenderLine.TenderCheck = tenderCheck;
                var formCheck = new FormTenderCheck();
                if (formCheck.FillTenderCheck(tenderCheck) == System.Windows.Forms.DialogResult.Cancel) {
                    throw new Exception("É necessário preencher os dados do cheque.");
                }
            }
            TenderLines.Add(tenderLine);

            return TenderLines;
        }

        
        /// <summary>
        /// Lê e mostra no ecran um recibo ou pagamento
        /// </summary>
        private void AccountTransactionGet(){
            string accountTransSerial = txtAccountTransSerial.Text;
            string accountTransDoc = txtAccountTransDoc.Text;
            double accountTransDocNumber = 0;
            double.TryParse(txtAccountTransDocNumber.Text, out accountTransDocNumber);

            AccountTransactionClear();
            var transLoaded = accountTransManager.LoadTransaction(accountTransSerial, accountTransDoc, accountTransDocNumber);
            if (! transLoaded ) {
                throw new Exception(string.Format("Não foi possivel carregar o documento {0} {1}/{2}.", accountTransDoc, accountTransSerial, accountTransDocNumber));
            }
            var accountTrans = accountTransManager.Transaction;
            //
            txtAccountTransDoc.Text = accountTrans.TransDocument;
            txtAccountTransDocCurrency.Text = accountTrans.BaseCurrency.CurrencyID;
            txtAccountTransDocNumber.Text = accountTrans.TransDocNumber.ToString();
            txtAccountTransPartyId.Text = accountTrans.Entity.PartyID.ToString();
            txtAccountTransDocDate.Text = accountTrans.CreateDate.ToShortDateString();
            //txtAccountTransPaymentId.Text = accountTrans.;
            txtAccountTransSerial.Text = accountTrans.TransSerial;
            //
            if (accountTrans.TenderLineItems.Count > 0) {
                var tenderLine = accountTrans.TenderLineItems[1];
                txtAccountTransPaymentId.Text = tenderLine.Tender.TenderID.ToString();
            }

            // Line 1
            if( accountTrans.Details.Count>0){
                int i = 1;
                var detail = accountTrans.Details[ ref i];
                txtAccountTransDocL1.Text = detail.DocID;
                txtAccountTransDocNumberL1.Text = detail.DocNumber.ToString();
                txtAccountTransDocValueL1.Text = detail.TotalPayedAmount.ToString();
                txtAccountTransSeriesL1.Text = detail.DocSerial;
                //
                // Line 2
                if (accountTrans.Details.Count > 1) {
                    i = 2;
                    detail = accountTrans.Details[ref i];
                    txtAccountTransDocL2.Text = detail.DocID;
                    txtAccountTransDocNumberL2.Text = detail.DocNumber.ToString();
                    txtAccountTransDocValueL2.Text = detail.TotalPayedAmount.ToString();
                    txtAccountTransSeriesL2.Text = detail.DocSerial;
                }
            }
            if (accountTrans.TransStatus == TransStatusEnum.stVoid) {
                tabAccount.BackgroundImage = Properties.Resources.stamp_Void;
            }
            else {
                tabAccount.BackgroundImage = null;
            }

            accountTrans = null;
        }


        /// <summary>
        /// Obtêm o documento por omissão para receibo ou pagamento
        /// </summary>
        /// <returns>O primeiro documetno encontrado para o tipo descrito</returns>
        private Document AccountTransGetDocument() {
            Document accountDoc = null;
            if (cmbRecPeg.SelectedIndex == 0) {
                // Primeiro documento disponivel para recebimento
                accountDoc = systemSettings.WorkstationInfo.Document.OfType<Document>().FirstOrDefault(x => x.TransDocType == DocumentTypeEnum.dcTypeAccount && x.UpdateTenderReport && x.AccountBehavior == AccountBehaviorEnum.abAccountSettlement && x.SignTenderReport == "+");
            }
            else {
                // Primeiro documento disponivel para pagamento
                accountDoc = systemSettings.WorkstationInfo.Document.OfType<Document>().FirstOrDefault(x => x.TransDocType == DocumentTypeEnum.dcTypeAccount && x.UpdateTenderReport && x.AccountBehavior == AccountBehaviorEnum.abAccountSettlement && x.SignTenderReport == "-");
            }
            return accountDoc;
        }

        /// <summary>
        /// Limpa a transação (recibo ou pagamento) do ecran e preenche alguns valores por omissão
        /// </summary>
        private void AccountTransactionClear() {
            if (cmbRecPeg.SelectedIndex < 0) cmbRecPeg.SelectedIndex = 0;

            var accountDoc = AccountTransGetDocument();
            if (accountDoc != null)
                txtAccountTransDoc.Text = accountDoc.DocumentID;
            else
                txtAccountTransDoc.Text = string.Empty;
            var externalSeries = systemSettings.DocumentSeries.OfType<DocumentsSeries>().FirstOrDefault(x => x.SeriesType == SeriesTypeEnum.SeriesExternal);
            if (externalSeries != null)
                txtAccountTransSerial.Text = externalSeries.Series;
            else
                txtAccountTransSerial.Text = string.Empty;
            txtAccountTransDocNumber.Text = "0";
            txtAccountTransDocCurrency.Text = systemSettings.BaseCurrency.CurrencyID;
            txtAccountTransPartyId.Text = string.Empty;
            txtAccountTransDocDate.Text = DateTime.Today.ToShortDateString();
            var tender = dsoCache.TenderProvider.GetFirstMoneyTender(TenderUseEnum.tndUsedOnBoth);
            if (tender != null)
                txtAccountTransPaymentId.Text = tender.TenderID.ToString();
            else
                txtAccountTransPaymentId.Text = string.Empty;
            txtAccountTransPaymentId.Text = dsoCache.PaymentProvider.GetFirstID().ToString();
            //
            AccountTransClearL1();
            AccountTransClearL2();
            //
            tabAccount.BackgroundImage = null;
        }

        private void AccountTransClearL1() {
            txtAccountTransSeriesL1.Text = string.Empty;
            txtAccountTransDocL1.Text = string.Empty;
            txtAccountTransDocNumberL1.Text = "0";
            txtAccountTransDocValueL1.Text = "0";
        }

        private void AccountTransClearL2() {
            txtAccountTransSeriesL2.Text = string.Empty;
            txtAccountTransDocL2.Text = string.Empty;
            txtAccountTransDocNumberL2.Text = "0";
            txtAccountTransDocValueL2.Text = "0";
        }

        private void btnAccountClearL1_Click(object sender, EventArgs e) {
            AccountTransClearL1();
        }
        
        private void btnAccountClearL2_Click(object sender, EventArgs e) {
            AccountTransClearL2();
        }

        private void cmbRecPeg_SelectedIndexChanged(object sender, EventArgs e) {
            switch (cmbRecPeg.SelectedIndex) {
                case 0: 
                    lblAccountPartyId.Text = "Cliente";
                    tabAccount.Text = "Recibo";
                    break;

                case 1: 
                    lblAccountPartyId.Text = "Fornecedor";
                    tabAccount.Text = "Pagamento";
                    break;
            }
            var accountDoc = AccountTransGetDocument();
            if (accountDoc != null)
                txtAccountTransDoc.Text = accountDoc.DocumentID;
            else
                txtAccountTransDoc.Text = string.Empty;
        }
        #endregion

        private void rbTransStock_CheckedChanged(object sender, EventArgs e) {
            tabTransModules.Visible = false;
            lblTransModules.Visible = false;
            txtTransGlobalDiscount.Enabled = false;
            //
            TransactionClear();
        }

        private void rbTransBuySell_CheckedChanged(object sender, EventArgs e) {
            tabTransModules.Visible = true;
            lblTransModules.Visible = true;
            txtTransGlobalDiscount.Enabled = true;
            //
            TransactionClear();
        }

        private void cmbTransPartyType_SelectedIndexChanged(object sender, EventArgs e) {
            TransactionClear();
        }

        private void btnPrint_Click(object sender, EventArgs e) {
            double transDocNumber =0;
            double.TryParse( txtTransDocNumber.Text, out transDocNumber);

            try {
                // Mostrar no ecran
                TransactionGet(false);
                //
                if (optPrintOptions0.Checked) {
                    //Imprimir com as regras default do Retail e caixa de diálogo
                    TransactionPrint(txtTransSerial.Text, txtTransDoc.Text, transDocNumber, chkPrintPreview.Checked);
                }
                else{
                    // Impressão customizada, exportação para PDF, ...
                    TransactionPrint2( txtTransSerial.Text, txtTransDoc.Text, transDocNumber);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }

        }


        /// <summary>
        /// Impressão normal via caixa de diálogo e regras do Retail
        /// </summary>
        /// <param name="transSerial"></param>
        /// <param name="transDoc"></param>
        /// <param name="transDocNumber"></param>
        /// <param name="printPreview"></param>
        private void TransactionPrint( string transSerial, string transDoc, double transDocNumber, bool printPreview ) {
            if (printPreview) {
                bsoItemTransaction.PrintTransaction(transSerial, transDoc, transDocNumber, PrintJobEnum.jobPreview, 1);
            }
            else {
                bsoItemTransaction.PrintTransaction(transSerial, transDoc, transDocNumber, PrintJobEnum.jobPrint, 1);
            }
        }

        private void tabItem_Click(object sender, EventArgs e) {

        }


        #region QuickSearch

        private void btnItemBrow_Click(object sender, EventArgs e) {
            ItemFind();
        }


        private static bool itemIsFindind = false;
        private bool ItemFind() {
            QuickSearch quickSearch = null;
            bool result = false;

            try {
                if (!itemIsFindind) {
                    itemIsFindind = true;
                    quickSearch = RTLAPIEngine.CreateQuickSearch(QuickSearchViews.QSV_Item, systemSettings.StartUpInfo.CacheQuickSearchItem);
                    clsCollection qsParams = new clsCollection();
                    qsParams.add(systemSettings.QuickSearchDefaults.WarehouseID, "@WarehouseID");
                    qsParams.add(systemSettings.QuickSearchDefaults.PriceLineID, "@PriceLineID");
                    qsParams.add(systemSettings.QuickSearchDefaults.LanguageID, "@LanguageID");
                    qsParams.add(systemSettings.QuickSearchDefaults.DisplayDiscontinued, "@Discontinued");
                    if (systemSettings.StartUpInfo.UseItemSearchAlterCurrency) {
                        qsParams.add(systemSettings.AlternativeCurrency.SaleExchange, "@ctxBaseCurrency");
                    }
                    else {
                        qsParams.add(systemSettings.QuickSearchDefaults.EuroConversionRate, "@ctxBaseCurrency");
                    }
                    quickSearch.Parameters = qsParams;

                    if (quickSearch.SelectValue()) {
                        result = true;
                        var itemId = quickSearch.ValueSelectedString();
                        ItemGet(itemId);
                    }
                    itemIsFindind = false;
                }
            }
            catch (Exception ex) {
                itemIsFindind = false;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally {

            }
            quickSearch = null;

            return result;
        }
        
        
        private void btnCustomerBrow_Click(object sender, EventArgs e) {
            CustomerFind();
        }


        private static bool customerIsFinding = false;
        private bool CustomerFind() {
            QuickSearch quickSearch = null;
            bool result = false;

            try {
                //show data for view with id=0: the title is fetched by the
                //quick search viewer.
                if (!customerIsFinding) {
                    customerIsFinding = true;

                    quickSearch = RTLAPIEngine.CreateQuickSearch(QuickSearchViews.QSV_Customer, systemSettings.StartUpInfo.CacheQuickSearchItem);

                    if (quickSearch.SelectValue()) {
                        double customerId = quickSearch.ValueSelectedDouble();
                        numCustomerId.Value = (decimal)customerId;
                        CustomerGet(customerId);
                        result = true;
                    }
                    else {
                        //Not found... do nothing
                    }
                    customerIsFinding = false;
                    quickSearch = null;
                }
            }
            catch (Exception ex) {
                customerIsFinding = false;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally {
            }

            return result;
        }

        private void btnSupplierBrow_Click(object sender, EventArgs e) {
            SupplierFind();
        }

        private static bool supplierIsFinding = false;
        private bool SupplierFind() {
            QuickSearch quickSearch = null;
            bool result = false;

            try {
                //show data for view with id=0: the title is fetched by the
                //quick search viewer.
                if (!supplierIsFinding) {
                    supplierIsFinding = true;

                    quickSearch = RTLAPIEngine.CreateQuickSearch(QuickSearchViews.QSV_Supplier, systemSettings.StartUpInfo.CacheQuickSearchItem);

                    if (quickSearch.SelectValue()) {
                        double supplierId = quickSearch.ValueSelectedDouble();
                        SupplierGet(supplierId);
                        result = true;
                    }
                    else {
                        //Not found... do nothing
                    }
                    supplierIsFinding = false;
                    quickSearch = null;
                }
            }
            catch (Exception ex) {
                supplierIsFinding = false;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            finally {
            }

            return result;
        }
        #endregion

        private void btnTransGetPrep_Click(object sender, EventArgs e) {
            try {
                TransactionGet(true);
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnSavePrep_Click(object sender, EventArgs e) {
            TransactionID result  = null;
            try {
                if (bsoItemTransaction.Transaction.TempTransIndex != 0) {
                    // Atualizar
                    result = TransactionEdit( true );
                }
                else {
                    result = TransactionInsert(true);
                }
                if (result != null) {
                    TransactionClear();
                    MessageBox.Show(string.Format("Colocado em preparação: {0}", result.ToString()), Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else {
                    MessageBox.Show(string.Format("Não foi possivel colocar em preparação: {0} {1}/{2}", 
                                                   txtTransSerial.Text, txtTransDoc.Text, txtTransDocNumber.Text), 
                                    Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        static bool tempTransactionIndexIsFinding = false;
        private double TempTransactionIndexFind(DocumentTypeEnum transDocType) {
            double result = 0;

            //show data for view with id=0: the title is fetched by the
            //quick search viewer.
            try {
                if (!tempTransactionIndexIsFinding) {
                    tempTransactionIndexIsFinding = true;
                    var quickSearch = RTLAPIEngine.CreateQuickSearch(QuickSearchViews.QSV_TempTransaction, false);

                    if (!systemSettings.SystemInfo.CanRestoreTempTranOnAll) {
                        quickSearch.ExtraWhereClause = "[Terminal]=" + systemSettings.WorkstationInfo.WorkstationID.ToString() + " AND [TransDocType]= " + ((int)transDocType).ToString();
                    }
                    else {
                        quickSearch.ExtraWhereClause = "[TransDocType]= " + ((int)transDocType).ToString();
                    }

                    if (quickSearch.SelectValue()) {
                        result = quickSearch.ValueSelectedDouble();
                    }
                    else {
                        result = -1;
                    }

                    tempTransactionIndexIsFinding = false;
                }
            }
            catch (Exception ex) {
                tempTransactionIndexIsFinding = false;
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
            return result;
        }

        private void btnTransactionFinalize_Click(object sender, EventArgs e) {
            try {
                string transDoc = txtTransDoc.Text;
                string transSerial = txtTransSerial.Text;
                double transdocNumber = 0;

                if (double.TryParse(txtTransDocNumber.Text, out transdocNumber)) {
                    if (bsoItemTransaction.FinalizeSuspendedTransaction(transSerial, transDoc, transdocNumber)) {
                        MessageBox.Show(string.Format("Documento finalizado: {0}", bsoItemTransaction.Transaction.TransactionID.ToString()),
                                         Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else {
                        MessageBox.Show(string.Format("Não foi possivel finalizar o documento suspenso: {0} {1}/{2}.", transDoc, transSerial, transdocNumber),
                                        Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    }
                }
                else {
                    MessageBox.Show( string.Format("O número do documento ({0}) não é válido.", txtTransDocNumber.Text),
                                     Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void btnAccoutTransPrint_Click(object sender, EventArgs e) {
            try {
                // Carregar o documento
                AccountTransactionGet();

                // Pré-visualizar ou Imprimir
                if (chkAccoutTransPrintPreview.Checked) {
                    accountTransManager.ExecuteFunction("PREVIEW", string.Empty);
                }
                else {
                    accountTransManager.ExecuteFunction("PRINT", string.Empty);
                }
            }
            catch (Exception ex) {
                MessageBox.Show(ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }



        private void TransactionPrint2(string transSerial, string transDoc, double transDocNumber ){
            clsLArrayObject objListPrintSettings;
            PrintSettings oPrintSettings = null;
            Document oDocument = null;
            PlaceHolders oPlaceHolders = new PlaceHolders();
            PrintSettings oDefaultPrintSettings = null;
    
            btnPrint.Enabled = false;
    
            oPlaceHolders = new PlaceHolders();
    
            try{
                oDocument = systemSettings.WorkstationInfo.Document[transDoc];    

                // Preencher as opções default
                oDefaultPrintSettings = new PrintSettings();

                // Pergunta impressora com a opção "Imprimir"
                oDefaultPrintSettings.AskForPrinter = optPrintOptions0.Checked;
                //
                // Imprimir segundo as regras do Retail
                oDefaultPrintSettings.UseIssuingOutput = true;
                //
                if(optPrintOptions0.Checked){
                    // Imprimir apenas
                    oDefaultPrintSettings.PrintAction = PrintActionEnum.prnActPrint;
                }
                else if( optPrintOptions1.Checked){
                    // Exportar para PDF
                    oDefaultPrintSettings.PrintAction = PrintActionEnum.prnActExportToFile;
                    oDefaultPrintSettings.ExportFileType = ExportFileTypeEnum.filePDF;
                    oDefaultPrintSettings.ExportFileFolder = oPlaceHolders.GetPlaceHolderPath(systemSettings.WorkstationInfo.PDFDestinationFolder);
                }
                //
                //Obter configurações de impressão na configuração de postos
                objListPrintSettings = printingManager.GetRangeTransactionPrintSettings(oDocument, ref oDefaultPrintSettings);
                //
                if( objListPrintSettings.getCount() > 0 ){
                    // Neste exemplo, vamos escolher a primeira configuração
                    // Se houverem mais configuradas, deve-se alterar para a pretendida
                    oPrintSettings = (PrintSettings)objListPrintSettings.item[0];
                    // Imprimir...
                    // Retorna falso em caso de erro

                    //ANTERIOR var printResult = bsoItemTransaction.PrintTransactionEx(transSerial, transDoc, transDocNumber, oPrintSettings );

                    if (chkPrintPreview.Checked) {
                        bsoItemTransaction.PrintTransaction(transSerial, transDoc, transDocNumber, PrintJobEnum.jobPreview, oPrintSettings.PrintCopies);
                    }
                    else {
                        bsoItemTransaction.PrintTransaction2(transSerial, transDoc, transDocNumber, PrintJobEnum.jobPrint, oPrintSettings.PrintCopies, oPrintSettings);
                    }
                }
                MessageBox.Show("Concluido.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch( Exception ex ){
                MessageBox.Show( ex.Message, Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation );
            }
            finally{
                btnPrint.Enabled = true;
                oDocument = null;
                oPlaceHolders = null;
            }
        }

        private void cmbItemColor_SelectedIndexChanged(object sender, EventArgs e) {
            cmbItemSize.ResetText();
            dataGridView1.Columns.Clear();
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView1.DataSource = GetGridDataColor();
            dataGridView1.Columns[0].Visible = false;
            dataGridView1.AutoSize = true;
            dataGridView1.Refresh();
        }

        private DataTable GetGridDataColor() {
            var mainProvider = RTLAPIEngine.DataManager.MainProvider;
            ItemColor color = (ItemColor)cmbItemColor.SelectedItem;
            string query = "SELECT Stock.ItemID, Stock.ColorID, Stock.SizeID, ItemSize.SequenceNumber, Stock.WarehouseID, Stock.PhysicalQty, Size.Description AS SizeDescription" +
                           " FROM ((Stock " +
                           " inner join Size on Stock.SizeID = Size.SizeID) " +
                           " inner join ItemSize on Stock.SizeID = ItemSize.SizeID AND Stock.ItemID = ItemSize.ItemID) " +
                           " WHERE Stock.ItemID = '" + mainProvider.SQLFormatter.SQLString(txtItemId.Text) + "' AND " +
                           " ColorID = " + mainProvider.SQLFormatter.SQLNumber(color.ColorID) +
                           " ORDER BY SequenceNumber, WarehouseID";

            var rs = mainProvider.Execute(query);

            DataTable dt = new DataTable();

            var keyCol = dt.Columns.Add("SizeId", typeof(int));
            keyCol.ColumnName = "Tamanho";
            dt.PrimaryKey = new DataColumn[] { keyCol };

            var colSizeDesc = dt.Columns.Add("Desc.", typeof(string));

            var warehouseList = dsoCache.WarehouseProvider.GetWarehouseList();
            foreach (Warehouse ware in warehouseList)
            {
                dt.Columns.Add(ware.WarehouseID.ToString(), typeof(double));
            }
            
            while (!rs.EOF) {
                var sizeId = (int)rs.Fields["SizeId"].Value;
                var warehouseId = (int)rs.Fields["WarehouseId"].Value;
                DataColumn col = dt.Columns[warehouseId.ToString()];
                
                var row = dt.Rows.Find(sizeId);
                if (row == null) {
                    row = dt.NewRow();
                    row[keyCol] = sizeId;
                    row[colSizeDesc] = rs.Fields["SizeDescription"].Value.ToString();
                    dt.Rows.Add(row);
                }
                row[col] = Math.Round((double)rs.Fields["PhysicalQty"].Value, 5);

                rs.MoveNext();
            }
            rs.Close();
            rs = null;
            
            return dt;
        }

        private void cmbItemSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            cmbItemColor.ResetText();
            dataGridView1.Columns.Clear();
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView1.DataSource = GetGridDataSize();
            dataGridView1.Columns[0].Visible = false;
            dataGridView1.AutoSize = true;
            dataGridView1.Refresh();
        }

        private DataTable GetGridDataSize()
        {
            var mainProvider = RTLAPIEngine.DataManager.MainProvider;
            ItemSize size = (ItemSize)cmbItemSize.SelectedItem;
            string query = "SELECT Stock.ItemID, Stock.ColorID, Stock.SizeID, ItemColor.SequenceNumber, Stock.WarehouseID, Stock.PhysicalQty, Color.Description AS ColorDescription" +
                           " FROM ((Stock " +
                           " inner join Color on Stock.ColorID = Color.ColorID) " +
                           " inner join ItemColor on Stock.ColorID = ItemColor.ColorID AND Stock.ItemID = ItemColor.ItemID) " +
                           " WHERE Stock.ItemID = '" + mainProvider.SQLFormatter.SQLString(txtItemId.Text) + "' AND " +
                           " Stock.SizeID = " + mainProvider.SQLFormatter.SQLNumber(size.SizeID) +
                           " ORDER BY SequenceNumber, WarehouseID";



            var rs = mainProvider.Execute(query);

            DataTable dt = new DataTable();

            var keyCol = dt.Columns.Add("ColorId", typeof(int));
            keyCol.ColumnName = "Cor";
            dt.PrimaryKey = new DataColumn[] { keyCol };

            var colColorDesc = dt.Columns.Add("Desc.", typeof(string));

            var warehouseList = dsoCache.WarehouseProvider.GetWarehouseList();
            foreach (Warehouse ware in warehouseList)
            {
                dt.Columns.Add(ware.WarehouseID.ToString(), typeof(double));
            }

            while (!rs.EOF)
            {
                var colorId = (int)rs.Fields["ColorId"].Value;
                var warehouseId = (int)rs.Fields["WarehouseId"].Value;
                DataColumn col = dt.Columns[warehouseId.ToString()];

                var row = dt.Rows.Find(colorId);
                if (row == null)
                {
                    row = dt.NewRow();
                    row[keyCol] = colorId;
                    row[colColorDesc] = rs.Fields["ColorDescription"].Value.ToString();
                    dt.Rows.Add(row);
                }
                row[col] = Math.Round((double)rs.Fields["PhysicalQty"].Value, 5);

                rs.MoveNext();
            }
            rs.Close();
            rs = null;

            return dt;
        }

        private void txtTenderID_TextChanged(object sender, EventArgs e) {

        }
    }
}