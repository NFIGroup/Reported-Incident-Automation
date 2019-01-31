using System.AddIn;
using System.Drawing;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;
////////////////////////////////////////////////////////////////////////////////
//
// File: WorkspaceAddIn.cs
//
// Comments:
//
// Notes: 
//
// Pre-Conditions: 
//
////////////////////////////////////////////////////////////////////////////////
namespace Reported_Incident_Automation
{
    public class WorkspaceAddIn : Panel, IWorkspaceComponent2
    {
       IRecordContext _recordContext;
        public static IGlobalContext _globalContext { get; private set; }
        IIncident _incidentRecord;
        public static int _reportID;
        public static string _onLoadFSARVal;
        public List<string> _affectedBusIds;
        public List<string> _allInternalIncidents;
        public static ProgressForm form = new ProgressForm();
        private System.Windows.Forms.Label label1;
        bool _showSRFormAfterSave = false;//set it to false, that mean no need to show SR/VIN form after save
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="inDesignMode">Flag which indicates if the control is being drawn on the Workspace Designer. (Use this flag to determine if code should perform any logic on the workspace record)</param>
        /// <param name="RecordContext">The current workspace record context.</param>
        public WorkspaceAddIn(bool inDesignMode, IRecordContext RecordContext, IGlobalContext GlobalContext,
                              int ReportID)
        {
            _globalContext = GlobalContext;
            if (!inDesignMode)
            {
                _recordContext = RecordContext;   
                _reportID = ReportID;
                _recordContext.DataLoaded += _recordContext_DataLoaded;
                _recordContext.Saved += _recordContext_Saved;
            }
            else
            {
                InitializeComponent();
            }
        }
        /// <summary>
        /// Method called by data load event. It does the following:
        /// 1> Get Onload FSAR value
        /// 2> Get all affected VIN mapped to currently opened reported Incident
        /// 3> If _showSRFormAfterSave is set then show SR/VIN form 
        /// </summary>
        private void _recordContext_DataLoaded(object sender, EventArgs e)
        {
            //Making onload async to reduce time of record load  
            var backgroundService = new BackgroundServiceUtil();
            backgroundService.RunAsync(() =>
            {
                //Get all affected VIN's of reported incident
                _affectedBusIds = new List<string>();
                _incidentRecord = (IIncident)_recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Incident);
                if (_incidentRecord != null)
                {
                    string[] response = RightNowConnectService.GetService().GetAffectedBus(_incidentRecord.ID);
                    if (response != null)
                    {
                        _affectedBusIds = response.ToList();
                    }
                }
                //Get FSAR value on Load
                _onLoadFSARVal = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "FSAR",_incidentRecord);
                string[] internalIncident = RightNowConnectService.GetService().GetAllInternalIncident(_incidentRecord.ID);
                if (internalIncident != null)
                {
                    RightNowConnectService.GetService().setIncidentField("CO","no_of_sr", internalIncident.Length.ToString(),_incidentRecord);
                }
            });

        }
        /// <summary>
        /// Method called by data Save event. It doesn the following task;
        /// 1> Check if FSAR is changed 
        /// 2> If FSAR is changed then it updates all internal incident (which SR virtually) with new FSAR record
        /// 
        /// </summary>
        private void _recordContext_Saved(object sender, System.EventArgs e)
        {
            _allInternalIncidents = new List<string>();
            string currentFSAR = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "FSAR",_incidentRecord);

            //If FSAR is changed then update all internal incident with same FSAR
            if(_onLoadFSARVal != currentFSAR)
            {
                string[] allInternalInc = RightNowConnectService.GetService().GetAllInternalIncident(_incidentRecord.ID);
                if (allInternalInc != null)
                {
                    _allInternalIncidents = allInternalInc.ToList();
                    RightNowConnectService.GetService().UpdateInternalIncident(_allInternalIncidents, currentFSAR);
                }
            }
            //If flag is set to show SR/VIN form on save then call selectSR_VIN function
            if (_showSRFormAfterSave)
            {
                _showSRFormAfterSave = false;
                System.Threading.Thread selectSrVinThread = new System.Threading.Thread(selectSR_VIN);
                selectSrVinThread.Start();
            }
        }
        /// <summary>
        /// Method called by the Add-In framework initialize in design mode.
        /// </summary>
        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "Reported Incident Automation Add-in";
            this.label1.Size = new System.Drawing.Size(20, 10);
            this.label1.TabIndex = 0;
            this.label1.Text = "Add-in to perform multi task automation like SR-VIN mapping, Warranty Check, FSAR related task";
            label1.Margin = new Padding(10);
            Controls.Add(this.label1);
            this.Size = new System.Drawing.Size(20, 10);
            this.ResumeLayout(false);
        }
        /// <summary>
        /// Method for unsubsribing events.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (null != _recordContext))
            {
                // unsubscribe from all the events
                _recordContext.DataLoaded -= _recordContext_DataLoaded;
                _recordContext.Saved -= _recordContext_Saved;
            }
            base.Dispose(disposing);
        }
        #region IAddInControl Members

        /// <summary>
        /// Method called by the Add-In framework to retrieve the control.
        /// </summary>
        /// <returns>The control, typically 'this'.</returns>
        public Control GetControl()
        {
            return this;
        }

        #endregion

        #region IWorkspaceComponent2 Members

        /// <summary>
        /// Sets the ReadOnly property of this control.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Method which is called when any Workspace Rule Action is invoked.
        /// </summary>
        /// <param name="ActionName">The name of the Workspace Rule Action that was invoked.</param>
        public void RuleActionInvoked(string ActionName)
        {
            if (_recordContext != null)
            {
               
                _incidentRecord = _recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Incident) as IIncident;

                switch (ActionName)
                {
                    case "singleWarrantyCheck":
                        form.Show();
                        
                        SingleWarrantyModel singleWarranty = new SingleWarrantyModel();
                        singleWarranty.WarrantyCheck(_incidentRecord);
                        form.Hide();
                        _recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
                        break;
                    case "massWarrantyCheck":
                        
                        form.Show();
                        MultiVinWarrantyModel massWarranty = new MultiVinWarrantyModel();
                        massWarranty.MultiVinWarrantyCheck(_incidentRecord);
                        form.Hide();
                        _recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
                        break;
                    case "selectVIN":
                        //If existing record
                        if(_incidentRecord != null && _incidentRecord.ID > 0)
                        {
                            selectSR_VIN();
                            //System.Threading.Thread selectSrVinThread = new System.Threading.Thread(selectSR_VIN);
                            //selectSrVinThread.Start();
                        }
                        //If new then first create the incident and then call function to show SR/VIN form
                        else
                        {
                            _showSRFormAfterSave = true;
                            _recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
                        }

                        break;
                    case "orderParts":
                        form.Show();
                        PartsOrderModel partsOrder = new PartsOrderModel();
                        partsOrder.OrderParts(_incidentRecord);
                        form.Hide();
                        _recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
                        break;
                    case "populateExpirationDate":
                        ExpirationDate expdate = new ExpirationDate(_recordContext);
                        expdate.SetExpirationDate();
                        break;
                    case "supplierWarrantyCheck":
                        // This functionality is taken care in CPM
                        /*form.Show();
                        SupplierWarrantyCheckModel supplierWarranty = new SupplierWarrantyCheckModel();
                        supplierWarranty.GetDetails(_incidentRecord,_recordContext);
                        form.Hide();*/
                        //_recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
                        break;
                    default:
                        break;
                }
            }
        }
        /// <summary>
        /// Method which is called when any Workspace Rule Condition is invoked.
        /// </summary>
        /// <param name="ConditionName">The name of the Workspace Rule Condition that was invoked.</param>
        /// <returns>The result of the condition.</returns>
        public string RuleConditionInvoked(string ConditionName)
        {
            return string.Empty;
        }

        /// <summary>
        /// Method which is called to to show SR/VIN search form.
        /// </summary>
        public void selectSR_VIN()
        {
            IOrganization orgRecord = (IOrganization)_recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Organization);
            string orgType = RightNowConnectService.GetService().getFieldFromOrgRecord("CO", "org_type", orgRecord);
            string[] srList = RightNowConnectService.GetService().GetOrgSR(orgRecord.ID, Convert.ToInt32(orgType));
            if (srList != null)
            {
                SalesReleaseVINSearchForm srVinForm = new SalesReleaseVINSearchForm(srList, _affectedBusIds, _recordContext);
                srVinForm.ShowDialog();
            }
            else
            {
                InfoLog("No Sales release found for Org " + orgRecord.Name +
                                ". Please make sure Customer submitting the incident has some sales relase mapped to it.");
            }
        }
        /// <summary>
        /// Method which is called to to show info/error message.
        /// </summary>
        /// <param name="message">Tesx message to be displayed in a pop-up</param>
        public static void InfoLog(string message)
        {
            form.Hide();
            MessageBox.Show(message);
        }
        /// <summary>
        /// Method which is called to clean the string.
        /// </summary>
        /// <param name="data">Data value</param>
        /// <returns>Cleaned data in string format</returns>
        public string dataCleanUp(string data)
        {
            return Regex.Replace(data, "(?<=^[^\"]*)\"|\"(?=[^\"]*$)", "");
        }
        #endregion
    }

    [AddIn("Workspace Factory AddIn", Version = "1.0.0.0")]
    public class WorkspaceAddInFactory : IWorkspaceComponentFactory2
    {
        #region IWorkspaceComponentFactory2 Members
        static public IGlobalContext _globalContext;
           
        private int _reportID;
        [ServerConfigProperty(DefaultValue = "100901")]
        public int ReportID
        {
            get { return _reportID; }
            set { _reportID = value; }
        }

        /// <summary>
        /// Method which is invoked by the AddIn framework when the control is created.
        /// </summary>
        /// <param name="inDesignMode">Flag which indicates if the control is being drawn on the Workspace Designer. (Use this flag to determine if code should perform any logic on the workspace record)</param>
        /// <param name="RecordContext">The current workspace record context.</param>
        /// <returns>The control which implements the IWorkspaceComponent2 interface.</returns>
        public IWorkspaceComponent2 CreateControl(bool inDesignMode, IRecordContext RecordContext)
        {           
            return new WorkspaceAddIn(inDesignMode, RecordContext, _globalContext, ReportID);
        }

        #endregion

        #region IFactoryBase Members

        /// <summary>
        /// The 16x16 pixel icon to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public Image Image16
        {
            get { return Reported_Incident_Automation.Properties.Resources.AddIn16; }
        }

        /// <summary>
        /// The text to represent the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Text
        {
            get { return "Reported Incident Automation"; }
        }

        /// <summary>
        /// The tooltip displayed when hovering over the Add-In in the Ribbon of the Workspace Designer.
        /// </summary>
        public string Tooltip
        {
            get { return "All Add-ins for Reporting Incident"; }
        }

        #endregion

        #region IAddInBase Members

        /// <summary>
        /// Method which is invoked from the Add-In framework and is used to programmatically control whether to load the Add-In.
        /// </summary>
        /// <param name="GlobalContext">The Global Context for the Add-In framework.</param>
        /// <returns>If true the Add-In to be loaded, if false the Add-In will not be loaded.</returns>
        public bool Initialize(IGlobalContext GlobalContext)
        {
            _globalContext = GlobalContext;
            return true;
        }
        #endregion
    }
}