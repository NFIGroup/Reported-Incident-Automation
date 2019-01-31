using Reported_Incident_Automation.RightNowService;
using RightNow.AddIns.AddInViews;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Reported_Incident_Automation
{
    public partial class SalesReleaseVINSearchForm : Form
    {
        private IRecordContext _recordContext;
        private static IIncident _incidentRecord;
        private static IContact _contactRecord;
        public string _woCatLabel;

        List<string> _existingAffectedVins = new List<string>();
        List<string> _affectedBusIds= new List<string>();
        List<string> _selectedIDs = new List<string>();
        List<string> _unSelectedIDs = new List<string>();

        ProgressForm form;
        int _buildCount = 0;

        public SalesReleaseVINSearchForm(string[] srList, List<string> existingAffectedVins,
                                         IRecordContext recordContext)
        {
            InitializeComponent();
            _recordContext = recordContext;
            _incidentRecord = (IIncident)_recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Incident);
            _contactRecord = (IContact)_recordContext.GetWorkspaceRecord(RightNow.AddIns.Common.WorkspaceRecordType.Contact);
            _existingAffectedVins = existingAffectedVins;

            //Set SR combox
            SR_Cmbbx.Items.Add(new KeyValuePair<int, string>(0, "[No Value]"));
            foreach (string sr in srList)
            {
                int key = Convert.ToInt32(sr.Split('~')[1]);
                string value = sr.Split('~')[0];
                SR_Cmbbx.Items.Add(new KeyValuePair<int, string>(key, value));
            }
            SR_Cmbbx.DisplayMember = "Value";
            SR_Cmbbx.ValueMember = "Key";

            form = new ProgressForm();

        }

        /// <summary>
        /// Search for VIN records based on SR No or VIN No
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SeacrhBtnClick(object sender, EventArgs e)
        {
            SelectAll_chkbx.Checked = false;
            ClearAll_Chkbx.Checked = false;
            
            var filters = new List<KeyValuePair<string, string>>();

            #region if SR and VIN null
            if ((SR_Cmbbx.SelectedItem == null || ((KeyValuePair<int, string>)SR_Cmbbx.SelectedItem).Key == 0)
                 && Vin_txtbx.Text == String.Empty && CustomerFleetNo_txtbx.Text==string.Empty)
            {
                if (dataGridView1 != null)
                {
                    clearDataGrid();//clear old view
                }
                MessageBox.Show("Select at least one Filter", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            #region if SR is not null
            if (SR_Cmbbx.SelectedItem != null && ((KeyValuePair<int, string>)SR_Cmbbx.SelectedItem).Key != 0)
            {
                filters.Add(new KeyValuePair<string, string>("Sales Release", ((KeyValuePair<int, string>)SR_Cmbbx.SelectedItem).Value));
            }
            #endregion

            #region if VIN is not null
            if (Vin_txtbx.Text != String.Empty)
            {
                filters.Add(new KeyValuePair<string, string>("VIN", Vin_txtbx.Text));
            }
            #endregion

            #region if Customer Fleet is not null
            if(CustomerFleetNo_txtbx.Text!=String.Empty)
            {
                filters.Add(new KeyValuePair<string, string>("Customer Fleet Number", CustomerFleetNo_txtbx.Text));
            }

            #endregion
            //Run and Get Report Data based on filter value
            CSVTable resultTable = RightNowConnectService.GetService().FilterSearch(filters);
            if (resultTable.Rows.Length > 0)
            {
                //Show report data in grid format
                ShowData(resultTable);
                //Select the checkbox for Vins that already exist 
                SelectExistingVins();
            }
            else
            {
                if (dataGridView1 != null)
                {
                    clearDataGrid();//clear old view
                }
                MessageBox.Show("No Result Found", "Attention", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            #endregion
        }

        /// <summary>
        /// Show the searched data in form (in grid format)
        /// </summary>
        /// <param name="resultTable">Data in CSVTable format</param>
        private void ShowData(CSVTable resultTable)
        {
            clearDataGrid();//clear old view

            DataTable dt = new DataTable();
            string[] tablerow = resultTable.Rows;
            string[] tablecolumn = resultTable.Columns.Split('~');

            foreach (string col in tablecolumn)
            {
                dt.Columns.Add(col);
            }
            foreach (string row in tablerow)
            {
                string[] rowdata = row.Split('~');
                dt.Rows.Add(rowdata);
            }
            
            DataGridViewCheckBoxColumn dgvcheckbox = new DataGridViewCheckBoxColumn();
            
            dgvcheckbox.ValueType = typeof(bool);
            dgvcheckbox.Name = "Select_CheckBox";
            dgvcheckbox.HeaderText = "Select VIN";
            dataGridView1.DataSource = dt;
            dataGridView1.Columns.Add(dgvcheckbox);
            dataGridView1.Columns[0].Visible = false;//hide the first column which is "Bus ID"
        }

        /// <summary>
        /// bydefault select existing affected bus
        /// </summary>
        private void SelectExistingVins()
        {
            if (_existingAffectedVins != null && _existingAffectedVins.Count > 0)
            {
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    foreach (string vin in _existingAffectedVins)
                    {
                        int busId = Convert.ToInt32(vin.Split('~')[0]);// First element is bus id and second is Incident_VIN ID
                        if (Convert.ToInt32(row.Cells["Bus ID"].Value) == busId)
                        {
                            row.Cells["Select_CheckBox"].Value = true;
                        }
                    }
                }
            }
        }
        /// <summary>
        /// clear the datagridview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearDataGrid()
        {
            dataGridView1.DataSource = null;
            dataGridView1.Columns.Clear();
            dataGridView1.Refresh();
        }
        /// <summary>
        /// Funtion to handle click of Build Button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildBtnClick(object sender, EventArgs e)
        {
            //Create new thread to show progress bar
            System.Threading.Thread th = new System.Threading.Thread(CreateRecords);
            th.Start();
            form.Show();
            //this.CreateRecords();
        }

        /// <summary>
        /// Create Internal Incident and FSAR_VIN records Function 
        /// </summary>
        private void CreateRecords()
        {
            //To not allow build event to add multiple VINs in case of warranty workorder type 
            //if VIN records already exist.
            if (_woCatLabel == "Warranty")
            {
                string[] response = RightNowConnectService.GetService().GetAffectedBus(_incidentRecord.ID);
                if (response != null)
                {
                    _affectedBusIds = response.ToList();
                }

                
                if (_affectedBusIds.Count > 0)
                {
                    form.Hide();
                    MessageBox.Show("For Warranty Work Order Category Multiple VINs cannot be added.");
                }
                else
                {
                    CompleteBuildProcess();
                }
            }
            else
            {
                CompleteBuildProcess();
            }
        }

        /// <summary>
        /// Function to Complete the Creation of Records as per selected VINs.
        /// </summary>
        public void CompleteBuildProcess()
        {
            //Get Unselected and Selected Vins
            foreach (DataGridViewRow _row in dataGridView1.Rows)
            {
                if (Convert.ToBoolean(_row.Cells["Select_CheckBox"].Value) == false)
                {
                    _unSelectedIDs.Add(_row.Cells["Bus ID"].Value.ToString());
                }
                if (Convert.ToBoolean(_row.Cells["Select_CheckBox"].Value) == true)
                {
                    _selectedIDs.Add(_row.Cells["Bus ID"].Value.ToString());
                }
            }

            try
            {
                #region Unselected Vins
                if (_unSelectedIDs != null && _unSelectedIDs.Count > 0)
                {
                    List<int> deleteVins = new List<int>();

                    if (_existingAffectedVins != null && _existingAffectedVins.Count > 0)
                    {
                        foreach (string uvin in _unSelectedIDs)
                        {
                            foreach (string evin in _existingAffectedVins)
                            {
                                int busId = Convert.ToInt32(evin.Split('~')[0]);// First element is bus id and second is Incident_VIN ID
                                if (Convert.ToInt32(uvin) == busId)
                                {
                                    // First element is bus id and second is Incident_VIN ID
                                    //deleteVins.Add(Convert.ToInt32(evin.Split('~')[1]));
                                    deleteVins.Add(Convert.ToInt32(evin.Split('~')[0]));
                                }
                            }
                        }
                        if (deleteVins.Count > 0)
                        {
                            RightNowConnectService.GetService().DeleteIncidentVIN(deleteVins, _incidentRecord.ID);
                        }
                    }
                }
                #endregion

                #region Selected Vins
                if (_selectedIDs != null && _selectedIDs.Count > 0)
                {
                    List<int> addVins = new List<int>();
                    for (int i = 0; i < _selectedIDs.Count; i++)
                    {
                        if (CheckIfVinExist(_selectedIDs[i]) == false) //to make sure duplicate records are not created
                        {
                            addVins.Add(Convert.ToInt32(_selectedIDs[i]));
                            _existingAffectedVins.Add(_selectedIDs[i]);
                        }
                    }
                    if (addVins.Count > 0)
                    {
                        //Get SR ID from any of selected VIN (as they all belong to same SR)
                        string[] srInfos = RightNowConnectService.GetService().getSRID(addVins);
                        List<int> vinsOfSameSR = new List<int>();
                        int oldSRID = -1;
                        int internalIncID = -1;
                        //Below logic is to handle is VIN belongs to different SR
                        //We have to create unique Internal incident based on SR 
                        for (int i= 0; i < srInfos.Length; i++)
                        {
                            //If first case
                            if(i==0)
                            {
                                //Store SR
                                oldSRID = Convert.ToInt32(srInfos[i].Split('~')[0]);
                                //Get Internal Incident for first SR
                                internalIncID = RightNowConnectService.GetService().CreateInternalIncident(_contactRecord.ID, oldSRID,
                                                                       _incidentRecord.ID,
                                                                       WorkspaceAddIn._onLoadFSARVal,
                                                                       (int)_contactRecord.OrgID,
                                                                       _incidentRecord);
                                vinsOfSameSR.Add(Convert.ToInt32(srInfos[i].Split('~')[1]));
                            }
                            else
                            {   
                                //Keep on adding VIN if SR is same for next VIN
                                if(oldSRID == Convert.ToInt32(srInfos[i].Split('~')[0]))
                                {
                                    vinsOfSameSR.Add(Convert.ToInt32(srInfos[i].Split('~')[1]));
                                }
                                //First save previously added VIN for prev SR, then clean the vinsOfSameSR list and repeat the process
                                else
                                {
                                    RightNowConnectService.GetService().createIncidentVIN(vinsOfSameSR, internalIncID);
                                    vinsOfSameSR.Clear();
                                    oldSRID = Convert.ToInt32(srInfos[i].Split('~')[0]);
                                    internalIncID = RightNowConnectService.GetService().CreateInternalIncident(_contactRecord.ID, oldSRID,
                                                                                                                _incidentRecord.ID,
                                                                                                                WorkspaceAddIn._onLoadFSARVal,
                                                                                                                (int)_contactRecord.OrgID,
                                                                                                                _incidentRecord);
                                    vinsOfSameSR.Add(Convert.ToInt32(srInfos[i].Split('~')[1]));
                                }
                            }
                        }
                        RightNowConnectService.GetService().createIncidentVIN(addVins, internalIncID);
                    }
 
                }
                #endregion

                if (this.IsHandleCreated)
                {

                    this.BeginInvoke(new Action(() =>
                    {

                        form.Hide();
                    }));

                }
                _buildCount++;
                MessageBox.Show(this, "Build Complete");
            }


            catch (Exception ex)
            {
                if (this.IsHandleCreated)
                {

                    this.BeginInvoke(new Action(() =>
                    {

                        form.Hide();
                    }));

                }
                WorkspaceAddIn.InfoLog("Exception in Build Button Event: " + ex.Message);
            }
        }


        /// <summary>
        /// Check if VIN already exit by comparing selected VIN against list of existing VINS
        /// </summary>
        /// <param name="vinId"> VIn ID</param>
        /// <param name="e"></param>
        private bool CheckIfVinExist(string vinId)
        {
            foreach (string existingAffectedVin in _existingAffectedVins)
            {
                if (existingAffectedVin.Split('~')[0] == vinId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check All the Checkboxes 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SelectAllCheckBoxChecked(object sender, EventArgs e)
        {
            if (SelectAll_chkbx.Checked == true)
            {
                foreach (DataGridViewRow checkrow in dataGridView1.Rows)
                {
                    checkrow.Cells["Select_CheckBox"].Value = true;
                }
                ClearAll_Chkbx.Checked = false;
            }
        }

        /// <summary>
        /// Uncheck all the Checkboxes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearAllCheckBoxChecked(object sender, EventArgs e)
        {
            if (ClearAll_Chkbx.Checked == true)
            {
                foreach (DataGridViewRow checkrow in dataGridView1.Rows)
                {
                    checkrow.Cells["Select_CheckBox"].Value = false;
                }
                SelectAll_chkbx.Checked = false;
            }
        }

        /// <summary>
        /// Keep Select All and Clear All checkboxes unchecked during form load
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncidentVinFormLoad(object sender, EventArgs e)
        {
            ClearAll_Chkbx.Checked = false;
            SelectAll_chkbx.Checked = false;

            string woCatID = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "work_order_category", _incidentRecord);
            if (woCatID != "")
            {
                _woCatLabel = RightNowConnectService.GetService().GetWOCategoryLabel(Convert.ToInt32(woCatID));
            }
            if (_woCatLabel == "Warranty" && _existingAffectedVins.Count==0)
            {
                MessageBox.Show("Work Order Category is selected as Warranty. Only one VIN can be added.");
                dataGridView1.MultiSelect = false;
                
            }
            if (_woCatLabel == "Warranty" && _existingAffectedVins.Count > 0)
            {
                MessageBox.Show("Work Order Category is selected as Warranty. Multiple VINs cannot be added.");
                this.Close();
            }
        }

        /// <summary>
        /// Uncheck Select All or Clear All checkboxes if any of the row checkbox is unchecked or checked respectively
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGridView1CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 3)
            {
                ClearAll_Chkbx.Checked = false;
                SelectAll_chkbx.Checked = false;
            }

            //to ensure multiple VINs are not selected for warranty workorder category.
            if (_woCatLabel == "Warranty")
            {
                int rowIndex = e.RowIndex;
                for (int i = 0; i < dataGridView1.Rows.Count; i++)
                {
                    if (rowIndex != i)
                    {
                        dataGridView1.Rows[i].Cells["Select_CheckBox"].Value = false;
                    }
                }
            }
        }

        /// <summary>
        /// Refresh Workspace on form close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void IncidentVinFormClosed(object sender, FormClosedEventArgs e)
        {
            if (_buildCount != 0)
            {
                _recordContext.ExecuteEditorCommand(RightNow.AddIns.Common.EditorCommand.Save);
            }
        }
    }
}
