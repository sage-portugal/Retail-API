﻿using System;
using RTLBL16;
using RTLSystem16;
using RTLBase16;
using System.Windows.Forms;

namespace RTLExtenderSample {
    class  TenderTransactionHandler : IDisposable {
            private ExtenderEvents headerEvents = null;
            private ExtenderEvents detailEvents = null;

            private TenderTransactionManager _tenderTransactionManager = null;
            private PropertyChangeNotifier _propChangeNotifier = null;
            private TenderTransaction _transaction = null;
        

            public void SetHeaderEventsHandler(ExtenderEvents e) {
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

            private void HeaderEvents_OnDispose() {
                // Dispose your objects
            }

            private void HeaderEvents_OnLoad(object Sender, ExtenderEventArgs e) {
                var trans = (TenderTransaction)e.get_data();

                ///... Code here

                e.result.Success = true;
            }

            private void HeaderEvents_OnNew(object Sender, ExtenderEventArgs e) {
                _transaction = (TenderTransaction)e.get_data();
            }

            private void HeaderEvents_OnDelete(object Sender, ExtenderEventArgs e) {
                System.Windows.Forms.MessageBox.Show("HeaderEvents_OnDelete: Acabei de anular.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            private void HeaderEvents_OnSave(object Sender, ExtenderEventArgs e) {
                System.Windows.Forms.MessageBox.Show("HeaderEvents_OnSave: Gravou.", Application.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            private void HeaderEvents_OnValidating(object Sender, ExtenderEventArgs e) {
                var propList = (ExtendedPropertyList)e.get_data();
                var forDeletion = (bool)propList.get_Value("ForDeletion");
                var transaction = (TenderTransaction)propList.get_Value("Data");

                if (!forDeletion) {
                    e.result.ResultMessage = "HeaderEvents_OnValidating: Validação do documento NOK. Não é permitido alterar.";
                    e.result.Success = false;
                }
                else {
                    e.result.ResultMessage = "HeaderEvents_OnValidating: Não pode anular documentos! Mas vou devolver TRUE para deixar anular.";
                    e.result.Success = true;
                }
            }

            void OnPropertyChanged(string PropertyID, ref object value, ref bool Cancel) {
                // HANDLE BSOTenderTransaction PROPERTY CHANGES HERE

                Console.WriteLine("OnPropertyChanged {0}={1}; Cancel={2}", PropertyID, value, Cancel);
                //Cancel = false;
            }


            /// <summary>
            /// Inicialização
            /// Podemos adicionar novas opções de menu aqui
            /// IN:
            ///     e.get_data(): ExtendedPropertyList
            ///     "PropertyChangeNotifier" = Evento que podemos subscrever para controlar quando uma propriedade é alterada
            ///     "TransactionManager" = BSOTenderTransaction; Controlador da transação em curso
            /// 
            /// OUT:
            ///     result.Sucess: true para sinalizar sucesso e carregar novos menus; false para cancelar
            ///     result.ResultMessage: Ignorado
            ///     result.set_data( ExtenderMenuItems ): Items de menu a carregar 
            /// </summary>
            /// <param name="Sender"></param>
            /// <param name="e"></param>
            void HeaderEvents_OnInitialize(object Sender, ExtenderEventArgs e) {
                var propList = (ExtendedPropertyList)e.get_data();
                _propChangeNotifier = (PropertyChangeNotifier)propList.get_Value("PropertyChangeNotifier");
                _propChangeNotifier.PropertyChanged += OnPropertyChanged;

                _tenderTransactionManager = (TenderTransactionManager)propList.get_Value("TransactionManager");
                //bsoItemTrans.WarningItemStock += BsoItemTrans_WarningItemStock;

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

            void HeaderEvents_OnMenuItem(object Sender, ExtenderEventArgs e) {
                var menuId = (string)e.get_data();
                var rnd = new Random();

                switch (menuId) {
                    case "mniXTrans1":
                        MessageBox.Show("mniXTrans1");
                        break;

                    case "mniXTrans2":
                        MessageBox.Show("mniXTrans2");
                        break;
                }
            }

            /// <summary>
            /// EXEMPLO DE VALIDAÇÃO NA LINHA   
            /// </summary>
            /// <param name="Sender"></param>
            /// <param name="e"></param>
            void DetailEvents_OnValidating(object Sender, ExtenderEventArgs e) {
                ExtendedPropertyList properties = (ExtendedPropertyList)e.get_data();
                TenderTransactionDetail _tenderTransactionDetail = (TenderTransactionDetail)properties.get_Value("Data");
                string errorMessage = string.Empty;

            }


            public void Dispose() {
                headerEvents = null;
                detailEvents = null;
                if (_tenderTransactionManager != null) {
                    _tenderTransactionManager = null;
                }
                _propChangeNotifier = null;
            }
        }
    }

