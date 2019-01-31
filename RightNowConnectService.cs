using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Windows.Forms;
using RightNow.AddIns.AddInViews;
using Reported_Incident_Automation.RightNowService;
using System.Linq;

namespace Reported_Incident_Automation
{
    class RightNowConnectService
    {
        private static RightNowConnectService _rightnowConnectService;
        private static object _sync = new object();
        private static RightNowSyncPortClient _rightNowClient;
        public List<RNObject> _incidentVINObjects = new List<RNObject>();
        private RightNowConnectService()
        {

        }
        public static RightNowConnectService GetService()
        {
            if (_rightnowConnectService != null)
            {
                return _rightnowConnectService;
            }

            try
            {
                lock (_sync)
                {
                    if (_rightnowConnectService == null)
                    {
                        // Initialize client with current interface soap url 
                        string url = WorkspaceAddIn._globalContext.GetInterfaceServiceUrl(ConnectServiceType.Soap);
                        EndpointAddress endpoint = new EndpointAddress(url);

                        BasicHttpBinding binding = new BasicHttpBinding(BasicHttpSecurityMode.TransportWithMessageCredential);
                        binding.Security.Message.ClientCredentialType = BasicHttpMessageCredentialType.UserName;

                        // Optional depending upon use cases
                        binding.MaxReceivedMessageSize = 1024 * 1024;
                        binding.MaxBufferSize = 1024 * 1024;
                        binding.MessageEncoding = WSMessageEncoding.Mtom;

                        _rightNowClient = new RightNowSyncPortClient(binding, endpoint);

                        BindingElementCollection elements = _rightNowClient.Endpoint.Binding.CreateBindingElements();
                        elements.Find<SecurityBindingElement>().IncludeTimestamp = false;
                        _rightNowClient.Endpoint.Binding = new CustomBinding(elements);
                        WorkspaceAddIn._globalContext.PrepareConnectSession(_rightNowClient.ChannelFactory);

                        _rightnowConnectService = new RightNowConnectService();
                    }

                }
            }
            catch (Exception e)
            {
                _rightnowConnectService = null;
                WorkspaceAddIn.InfoLog(e.Message);
            }
            return _rightnowConnectService;
        }

        /// <summary>
        /// Return individual fields as per query
        /// </summary>
        /// <param name="ApplicationID"></param>
        /// <param name="Query"></param>
        /// <returns> array of string delimited by '~'</returns>
        private string[] GetRNData(string ApplicationID, string Query)
        {
            string[] rnData = null;
            ClientInfoHeader hdr = new ClientInfoHeader() { AppID = ApplicationID };

            byte[] output = null;
            CSVTableSet data = null;

            try
            {
                data = _rightNowClient.QueryCSV(hdr, Query, 1000, "~", false, false, out output);
                string dataRow = String.Empty;
                if (data != null && data.CSVTables.Length > 0 && data.CSVTables[0].Rows.Length > 0)
                {
                    return data.CSVTables[0].Rows;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog(ex.Message);
            }
            return rnData;
        }

        /// <summary>
        /// Funtion to Run the report
        /// </summary>
        /// <param name="report">AnalyticsReport info like report ID and Filter detail</param>
        /// <returns>CSVTableSet</returns>
        public CSVTableSet RunReport(AnalyticsReport report)
        {
            CSVTableSet reportdata = null;
            byte[] bytearray = null;
            try
            {
                ClientInfoHeader hdr = new ClientInfoHeader() { AppID = "Get Report Data" };
                reportdata = _rightNowClient.RunAnalyticsReport(hdr, report, 10000, 0, "~", false, true, out bytearray);
                if (reportdata != null && reportdata.CSVTables.Length > 0)
                {
                    return reportdata;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in RunReport: " + ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Get Config Value based on lookupName
        /// </summary>
        /// <param name="configLookupName"></param>
        /// <returns>config value</returns>
        public string GetConfigValue(string configLookupName)
        {
            string query = "select Configuration.Value from Configuration where lookupname = '" + configLookupName + "'";
            string[] resultSet = GetRNData("Configuartion Value", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                var jsonTrim = resultSet[0].Replace("\"\"", "\"");

                // jsonString has extra " at start, end and each " 
                int i = jsonTrim.IndexOf("\"");
                int j = jsonTrim.LastIndexOf("\"");
                var finalJson = jsonTrim.Substring(i + 1, j - 1);
                finalJson = finalJson.Replace("@xmlns", "xmlns");

                return finalJson;
            }
            return null;
        }
        /// <summary>
        /// Return all parts of open incident/eclaim as per query
        /// </summary>
        /// <param name="reportedIncID">Reported Incident ID</param>
        /// <returns> array of string delimited by '~'</returns>
        public string[] GetPartsInfo(int reportedIncID)
        {
            string query = "SELECT ID, part_nmbr, qty, source_type.LookupName, ship_set" +
                           " FROM CO.Parts p WHERE nf_sales_order IS NULL AND incident = " + reportedIncID;
            string[] resultSet = GetRNData("Parts Info", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet;
            }
            return null;
        }
        /// <summary>
        /// Get ebs id of site
        /// </summary>
        /// <param name="siteID"></param>
        /// <returns> string </returns>
        public string GetEbsID(int siteID)
        {
            string query = String.Format("SELECT ebs_id_site FROM CO.Site WHERE ID = {0} limit 1", siteID);
            string[] resultSet = GetRNData("Get Ebs Site Id", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return null;
        }
        /// <summary>
        /// Get ebs id of Org
        /// </summary>
        /// <param name="orgID"></param>
        /// <returns> string </returns>
        public string GetEbsOrgID(int orgID)
        {
            string query = "SELECT CustomFields.CO.ebs_id_org FROM Organization WHERE ID = " + orgID;
            string[] resultSet = GetRNData("Get Ebs Org Id", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return "";
        }

        public string getBus(int ebsIDOrg)
        {
            string query = "SELECT ID FROM Organization WHERE CustomFields.CO.ebs_id_org = " + ebsIDOrg;
            string[] resultSet = GetRNData("Get Org Id", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return "";
        }


        /// <summary>
        /// Get Org ID based on EBS ORG ID
        /// </summary>
        /// <param name="ebsIDOrg"></param>
        /// <returns> string </returns>
        public string GetOrgID(int ebsIDOrg)
        {
            string query = "SELECT ID FROM Organization WHERE CustomFields.CO.ebs_id_org = " + ebsIDOrg;
            string[] resultSet = GetRNData("Get Org Id", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return "";
        }
        /// <summary>
        /// Get Bus owner org ebs id
        /// </summary>
        /// <param name="orgID"> Reported Incident Org ID</param>
        /// <returns> string </returns>
        public string GetBusOwnerEbsID(int orgID)
        {
            string query = "SELECT CustomFields.CO.ebs_id_org FROM Organization WHERE ID = " + orgID;
            string[] resultSet = GetRNData("Get Org Ebs ID", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return null;
        }
        /// <summary>
        /// Get Order type label
        /// </summary>
        /// <param name="orderTypeID"></param>
        /// <returns> order type label</returns>
        public string GetOrderTypeName(int orderTypeID)
        {
            string query = String.Format("SELECT Name FROM CO.OrderType WHERE ID = {0} limit 1", orderTypeID);
            string[] resultSet = GetRNData("Order Type Info", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return null;
        }
        /// <summary>
        /// Update All parts that been order with sales order number of EBS
        /// </summary>
        /// <param name="partsInfo">List of Parts Info</param>
        /// <param name="salesOrderNum">NF sales order number</param>
        /// <returns></returns>
        public void UpdatePartsRecord(string[] partsInfo, string salesOrderNum)
        {
            try
            {
                List<GenericField> partsFields = new List<GenericField>();
                partsFields.Add(createGenericField("nf_sales_order", createStringDataValue(salesOrderNum), DataTypeEnum.STRING));

                List<RNObject> partsObjects = new List<RNObject>();

                foreach (string partInfo in partsInfo)
                {
                    GenericObject partsObj = new GenericObject { ObjectType = new RNObjectType { Namespace = "CO", TypeName = "Parts" } };
                    partsObj.ID = new ID { id = Convert.ToInt32(partInfo.Split('~')[0]), idSpecified = true };
                    partsObj.GenericFields = partsFields.ToArray();

                    partsObjects.Add(partsObj);
                }
                callBatchJob(getUpdateMsg(partsObjects));
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in Updating Parts Record: " + ex.Message);
            }
            return;
        }
        /// <summary>
        /// Return Bus VIN and SR number
        /// </summary>
        /// <param name="reportingIncID"> Currently opened reporting Incident ID</param>
        /// <returns> Bus info separted by '~'</returns>
        public string[] getBusInfo(int reportingIncID)
        {
            string queryString = "SELECT IncVin.Bus.Vin as vin," +
                                 " IncVin.Incident.CustomFields.CO.SalesRelease.sr_nmbr as srNUM, IncVin.ID" +
                                 " FROM CO.Incident_VIN IncVin WHERE" +
                                 " IncVin.Incident.CustomFields.CO.reporting_incident = " + reportingIncID;
            String[] resultSet = GetRNData("Get Bus Info", queryString);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet;
            }
            return null;
        }


        public string[] getBusInfoIV(int IncVINID)
        {
            string queryString = "SELECT IncVin.Bus.Vin as vin," +
                                 " IncVin.Incident.CustomFields.CO.SalesRelease.sr_nmbr as srNUM, IncVin.ID" +
                                 " FROM CO.Incident_VIN IncVin WHERE" +
                                 " ID = " + IncVINID;
            String[] resultSet = GetRNData("Get Bus Info IV", queryString);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet;
            }
            return null;
        }


        public string[] getEWRID(string ewrCode)
        {
            string queryString = "SELECT e.ID FROM Other.EWR_NHTSA e WHERE e.Service_Part_code  = '" + ewrCode + "'";
            String[] resultSet = GetRNData("Get EWR ", queryString);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet;
            }
            return null;
        }

        /// <summary>
        /// Get All Internal Incident ID and Sales Release ID
        /// </summary>
        /// <param name="incID"></param>
        /// <returns></returns>
        public string[] GetInternalIncident(int incID)
        {
            try
            {
                string query = "select I.ID, I.CustomFields.CO.SalesRelease, I.CustomFields.CO.SalesRelease.sr_nmbr from Incident I" +
                    " where I.CustomFields.CO.reporting_incident=" + incID;
                string[] resultset = GetRNData("Get Intenal Incident Info", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in getting Internal Incident: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Get the affetced VINS for each SR
        /// </summary>
        /// <param name="incID"></param>
        /// <returns></returns>
        public string[] GetVins(int incID)
        {
            try
            {
                string query = "select IncVin.Bus.Vin, IncVin.ID from CO.Incident_VIN IncVin where IncVin.Incident = " + incID;
                string[] resultset = GetRNData("Get Bus Info", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in getting exisitng affected VIN IDs: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Get all affected Bus for all the Internal Incidents
        /// </summary>
        /// <param name="incID"></param>
        /// <returns> array of bus id in string format</returns>
        public string[] GetAffectedBus(int incID)
        {
            try
            {
                string query = "select V.Bus, V.ID from CO.Incident_VIN V where " +
                               "V.Incident.CustomFields.CO.reporting_incident = " + incID;
                string[] resultset = GetRNData("Get existing affected bus", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in getting exisitng affected Bus IDs: " + ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Get SR ID based on VIN id
        /// </summary>
        /// <param name="busID"></param>
        /// <returns> SR ID</returns>
        public string[] getSRID(List<int> busID)
        {
            try
            {
                string query = "SELECT sales_release.ID, ID  FROM CO.Bus WHERE ID IN (" + String.Join(",", busID) + ")";
                string[] resultset = GetRNData("Get SR ID", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in SR ID based on Bus ID: " + ex.Message);
            }
            return null;
        }
        /// <summary>
        /// Get Organization's list of all SR
        /// </summary>
        /// <param name="OrgID">Org ID</param>
        /// <returns> string[] List of SalesRelease Info seperated by ~ </returns>
        public string[] GetOrgSR(int OrgID, int orgTypeID)
        {
            string query="";
            switch (orgTypeID)
            {
                case 1://customer case
                    query = "select S.sr_nmbr, S.ID from CO.SalesRelease S where S.organization=" + OrgID;
                    break;
                case 3://Opeartor case
                    query = "SELECT b.sales_release.sr_nmbr, b.sales_release.ID FROM CO.Bus b WHERE b.org_operator =" +OrgID 
                          + " GROUP BY b.sales_release.ID";
                    break;
            }
            if (query != "")
            {
                string[] resultset = GetRNData("Get Org SR", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            return null;
        }

        /// <summary>
        /// Get WO Category Label based on ID
        /// </summary>
        /// <param name="itemID">WO Category ID</param>
        /// <returns> sLabel in String</returns>
        public string GetWOCategoryLabel(int itemID)
        {
            string query = "Select Name From TOA.WO_Category Where ID = " + itemID;
            string[] resultset = GetRNData("Get WO Category Label", query);
            if (resultset != null && resultset.Length > 0)
            {
                return resultset[0];
            }
            return "";
        }
        /// <summary>
        /// Get Result of Report based on Filter's passed
        /// </summary>
        /// <param name="filters">List of filter name and Values</param>
        /// <returns>CSVTable</returns>
        public CSVTable FilterSearch(List<KeyValuePair<string, string>> filters)
        {
            CSVTable table = null;
            try
            {
                AnalyticsReport anlyticsreport = new AnalyticsReport { ID = new ID { id = WorkspaceAddIn._reportID, idSpecified = true } };
                List<AnalyticsReportFilter> arFilter = new List<AnalyticsReportFilter>();

                foreach (KeyValuePair<string, string> filter in filters)
                {
                    arFilter.Add(new AnalyticsReportFilter
                    {
                        Name = filter.Key,
                        Operator = new NamedID
                        {
                            ID = new ID
                            {
                                id = 1,
                                idSpecified = true
                            }
                        },
                        Values = new string[] { filter.Value }
                    });
                }
                anlyticsreport.Filters = arFilter.ToArray();
                CSVTableSet tableset = RunReport(anlyticsreport);
                if (tableset.CSVTables.Length > 0)
                {
                    table = tableset.CSVTables[0];
                    return table;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in search " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Batch Request to Delete Incident_VIN records
        /// </summary>
        /// <param name="delete_id">List of Vins need to be deleted</param>
        /// <param name="rID">Reported Inc ID</param>
        /// <returns></returns>
        public void DeleteIncidentVIN(List<int> deleteVins, int rID)
        {
            try
            {
                List<int> internalIncIDs = new List<int>();
                List<RNObject> deleteObject = new List<RNObject>();
                //Get Incident_VIN IDs that need to be deletd based on Reported Inc ID and Bus ID
                string query = "SELECT IV.ID, IV.Incident.ID as iiID FROM CO.Incident_VIN IV"
                             + " WHERE Bus IN (" + String.Join(",", deleteVins) + ")"
                             + " AND IV.Incident.Customfields.CO.reporting_incident ="+ rID;
                string[] resultset = GetRNData("Get Incident_VIN info", query);
                
                foreach (string result in resultset)
                {
                    GenericObject genObj = new GenericObject
                    {
                        ObjectType = new RNObjectType
                        {
                            Namespace = "CO",
                            TypeName = "Incident_VIN"
                        }
                    };
                    genObj.ID = new ID
                    {
                        id = Convert.ToInt32(result.Split('~')[0]),
                        idSpecified = true
                    };

                    deleteObject.Add(genObj);
                    internalIncIDs.Add(Convert.ToInt32(result.Split('~')[1]));//store the internal inc ID
                }
 
                //BatchResponseItem[] batchRes = rspc.Batch(clientInfoHeader, requestItems);
                callBatchJob(getDestroyMsg(deleteObject));
                
                //Get unique internal Inc ID
                internalIncIDs = internalIncIDs.Distinct().ToList();
                
                foreach (int internalIncID in internalIncIDs)
                {
                    IncidentVinCountForInternalInc(internalIncID);//check if Internal incident is empty, if so then delete that
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in Deleting Incident_VIN record: " + ex.Message);
            }
            return;
        }
        /// <summary>
        /// Check if Internal incident has any Incident_VIN child record, if not then delete internal incident
        /// </summary>
        /// <param name="internalIncID"> Reported Incident Org ID</param>
        /// <returns> </returns>
        public void IncidentVinCountForInternalInc(int internalIncID)
        {
            string query = "SELECT count(ID) as count FROM CO.Incident_VIN WHERE incident = " + internalIncID;
            string[] resultSet = GetRNData("Get incident_VIN count", query);
            
            if (resultSet != null && resultSet.Length > 0)
            {
                
                if (resultSet[0] == "0")//if count is 0 then delete internal incident too
                {
                    
                    List <int> incId = new List<int>();
                    incId.Add(internalIncID);
                    
                    DeleteInternalIncident(incId);
                }
            }
            return;
        }
        /// <summary>
        /// Delete Internal Incident Records
        /// </summary>
        /// <param name="deleteIncIDs"></param>
        /// <returns></returns>
        public void DeleteInternalIncident(List<int> deleteIncIDs)
        {
            try
            {
                List<RNObject> deleteObject = new List<RNObject>();
                for (int i = 0; i < deleteIncIDs.Count; i++)
                {

                    Incident incObj = new Incident();
                    incObj.ID = new ID
                    {
                        id = deleteIncIDs[i],
                        idSpecified = true
                    };

                    deleteObject.Add(incObj);
                }

                //BatchResponseItem[] batchRes = rspc.Batch(clientInfoHeader, requestItems);
                callBatchJob(getDestroyMsg(deleteObject));
            }
            catch (Exception ex)
            {
                //SR mean internal incident
                MessageBox.Show("Exception in Deleting Internal Incident record: " + ex.Message);
            }
            return;
        }
        /// <summary>
        /// Create Internal Incident Records
        /// </summary>
        /// <param name="contactID"></param>
        /// <param name="srID"></param>
        /// <param name="reportingIncID"></param>
        /// <param name="fsarID"></param>
        /// <param name="orgID"></param>
        /// <returns></returns>
        public int CreateInternalIncident(int contactID, int srID, int reportingIncID, string fsarID, int orgID, IIncident incidentRecord)
        {
            try
            {
                //Check if it exist in order to avoid duplicate record
                string response = checkIfIncidentExistForSR(reportingIncID, srID);
                if (response != null)
                    return Convert.ToInt32(response);

                /*Set OOTB fields*/
                Incident newIncident = new Incident();
                IncidentContact primarycontact = new IncidentContact { Contact = new NamedID { ID = new ID { id = contactID, idSpecified = true } } };
                newIncident.PrimaryContact = primarycontact;
                newIncident.Organization = new NamedID { ID = new ID { id = orgID, idSpecified = true } };

                /*Set Custom Attributes*/
                List<GenericField> customAttributes = new List<GenericField>();
                customAttributes.Add(createGenericField("SalesRelease", createNamedIDDataValue(srID), DataTypeEnum.NAMED_ID));
                customAttributes.Add(createGenericField("reporting_incident", createNamedIDDataValue(reportingIncID), DataTypeEnum.NAMED_ID));
                DataValue fsarData = (fsarID == string.Empty) ? null : createNamedIDDataValue(Convert.ToInt32(fsarID));
                customAttributes.Add(createGenericField("FSAR", fsarData, DataTypeEnum.NAMED_ID));
                GenericObject customAttributeobj = genericObject(customAttributes.ToArray(), "IncidentCustomFieldsc");
                GenericField caPackage = createGenericField("CO", createObjDataValue(customAttributeobj), DataTypeEnum.OBJECT);

                /*Set Custom fields*/
                List<GenericField> customFields = new List<GenericField>();
                customFields.Add(createGenericField("incident_type", createNamedIDDataValueForName("Internal Incident"), DataTypeEnum.NAMED_ID));//55 is id of "Internal incident"
                GenericObject customfieldobj = genericObject(customFields.ToArray(), "IncidentCustomFieldsc");
                GenericField cfpackage = createGenericField("c", createObjDataValue(customfieldobj), DataTypeEnum.OBJECT);

                newIncident.CustomFields = genericObject(new[] { caPackage, cfpackage }, "IncidentCustomFields");

                ClientInfoHeader hdr = new ClientInfoHeader() { AppID = "Create Internal Incident" };
                RNObject[] resultobj = _rightNowClient.Create(hdr, new RNObject[] { newIncident }, new CreateProcessingOptions { SuppressExternalEvents = false, SuppressRules = false });
                if (resultobj != null)
                {
                    //Once SR is added, increment the SR number in no_of_sr field of Reported Incident
                    string oldSRNo = getFieldFromIncidentRecord("CO", "no_of_sr", incidentRecord);//old sr number
                    int currentSrNo = (oldSRNo != "") ? Convert.ToInt32(oldSRNo) + 1 : 1;//add 1
                    setIncidentField("CO", "no_of_sr", currentSrNo.ToString(), incidentRecord);//update new SR num

                    return Convert.ToInt32(resultobj[0].ID.id);
                }
            }
            catch (Exception ex)
            {

                WorkspaceAddIn.InfoLog("Exception in Internal incident Create: " + ex.Message);
            }
            return 0;
        }
        /// <summary>
        /// Method which is called to get value of a custom field of Org record.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="fieldName">The name of the custom field.</param>
        /// <returns>Value of the field</returns>
        public string getFieldFromOrgRecord(string packageName, string fieldName, IOrganization orgRecord)
        {
            string value = "";
            IList<ICustomAttribute> incCustomAttributes = orgRecord.CustomAttributes;

            foreach (ICustomAttribute val in incCustomAttributes)
            {
                if (val.PackageName == packageName)//if package name matches
                {
                    if (val.GenericField.Name == packageName + "$" + fieldName)//if field matches
                    {
                        if (val.GenericField.DataValue.Value != null)
                        {
                            value = val.GenericField.DataValue.Value.ToString();
                            break;
                        }
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// Method which is called to get value of a custom field of Incident record.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="fieldName">The name of the custom field.</param>
        /// <returns>Value of the field</returns>
        public string getFieldFromIncidentRecord(string packageName, string fieldName, IIncident incidentRecord)
        {
            string value = "";
            if (packageName == "c")
            {
                IList<ICfVal> incCustomFields = incidentRecord.CustomField;
                int fieldID = GetCustomFieldID(fieldName);
                foreach (ICfVal val in incCustomFields)
                {
                    if (val.CfId == fieldID)
                    {
                        return val.ValInt.Value.ToString();
                    }
                }
            }
            else
            {
                IList<ICustomAttribute> incCustomAttributes = incidentRecord.CustomAttributes;

                foreach (ICustomAttribute val in incCustomAttributes)
                {
                    if (val.PackageName == packageName)//if package name matches
                    {
                        if (val.GenericField.Name == packageName + "$" + fieldName)//if field matches
                        {
                            if (val.GenericField.DataValue.Value != null)
                            {
                                value = val.GenericField.DataValue.Value.ToString();
                                break;
                            }
                        }
                    }
                }
            }
            return value;
        }

        /// <summary>
        /// Method to get custom field id by name
        /// </summary>
        /// <param name="fieldName">Custom Field Name</param>
        public int GetCustomFieldID(string fieldName)
        {
            IList<IOptlistItem> CustomFieldOptList = WorkspaceAddIn._globalContext.GetOptlist((int)RightNow.AddIns.Common.OptListID.CustomFields);//92 returns an OptList of custom fields in a hierarchy
            foreach (IOptlistItem CustomField in CustomFieldOptList)
            {
                if (CustomField.Label == fieldName)//Custom Field Name
                {
                    return (int)CustomField.ID;//Get Custom Field ID
                }
            }
            return -1;
        }

        /// <summary>
        /// Method which is use to set incident field 
        /// </summary>
        /// <param name="pkgName">package name of custom field</param>
        /// <param name="fieldName">field name</param>
        /// <param name="value">value of field</param>
        public void setIncidentField(string pkgName, string fieldName, string value, IIncident incidentRecord)
        {
            if (pkgName == "c")
            {
                IList<ICfVal> incCustomFields = incidentRecord.CustomField;
                int fieldID = GetCustomFieldID(fieldName);
                foreach (ICfVal val in incCustomFields)
                {
                    if (val.CfId == fieldID)
                    {
                        switch (val.DataType)
                        {
                            case (int)RightNow.AddIns.Common.DataTypeEnum.BOOLEAN_LIST:
                            case (int)RightNow.AddIns.Common.DataTypeEnum.BOOLEAN:
                                if (value == "1" || value.ToLower() == "true")
                                {
                                    val.ValInt = 1;
                                }
                                else if (value == "0" || value.ToLower() == "false")
                                {
                                    val.ValInt = 0;
                                }
                                break;
                        }

                    }
                }
            }
            else
            {
                IList<ICustomAttribute> incCustomAttributes = incidentRecord.CustomAttributes;

                foreach (ICustomAttribute val in incCustomAttributes)
                {
                    if (val.PackageName == pkgName)
                    {
                        if (val.GenericField.Name == pkgName + "$" + fieldName)
                        {
                            switch (val.GenericField.DataType)
                            {
                                case RightNow.AddIns.Common.DataTypeEnum.BOOLEAN:
                                    if (value == "1" || value.ToLower() == "true")
                                    {
                                        val.GenericField.DataValue.Value = true;
                                    }
                                    else if (value == "0" || value.ToLower() == "false")
                                    {
                                        val.GenericField.DataValue.Value = false;
                                    }
                                    break;
                                case RightNow.AddIns.Common.DataTypeEnum.INTEGER:
                                    if (value.Trim() == "" || value.Trim() == null)
                                    {
                                        val.GenericField.DataValue.Value = null;
                                    }
                                    else
                                    {
                                        val.GenericField.DataValue.Value = Convert.ToInt32(value);
                                    }
                                    break;
                                case RightNow.AddIns.Common.DataTypeEnum.STRING:
                                    val.GenericField.DataValue.Value = value;
                                    break;
                                case RightNow.AddIns.Common.DataTypeEnum.DATETIME:
                                    val.GenericField.DataValue.Value = Convert.ToDateTime(value);
                                    break;
                                case RightNow.AddIns.Common.DataTypeEnum.ID:
                                    val.GenericField.DataValue.Value = (value == "") ? null : value;
                                    break;
                            }
                        }
                    }
                }
            }
            return;
        }

        /// <summary>
        /// Get all affected Bus for all the Internal Incidents
        /// </summary>
        /// <param name="incID"></param>
        /// <returns> array of bus id in string format</returns>
        public string checkIfIncidentExistForSR(int incID, int srID)
        {
            try
            {
                string query = "Select ID from Incident where CustomFields.CO.reporting_incident = " + incID +
                               " AND CustomFields.CO.SalesRelease = " + srID;
                string[] resultset = GetRNData("Get Incident based on SR", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset[0];
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in getting Internal Incident for SR: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Set Custom Fields for Internal Incident while Creation
        /// </summary>
        /// <param name="srID"></param>
        /// <param name="reportingIncID"></param>
        /// <param name="newinc"></param>
        /*public void SetCustomField(int srID, int reportingIncID, Incident newInc)
        {
            try
            {
                List<GenericField> customFields = new List<GenericField>();
                customFields.Add(createGenericField("SalesRelease", createNamedIDDataValue(srID), DataTypeEnum.NAMED_ID));
                customFields.Add(createGenericField("reporting_incident", createNamedIDDataValue(reportingIncID), DataTypeEnum.NAMED_ID));
                customFields.Add(createGenericField("incident_type", createNamedIDDataValueForName("Internal incident"), DataTypeEnum.NAMED_ID));//55 is id of "Internal incident"

                GenericObject customfieldobj = genericObject(customFields.ToArray(), "IncidentCustomFieldsc");
                GenericField package = createGenericField("CO", createObjDataValue(customfieldobj), DataTypeEnum.OBJECT);

                newInc.CustomFields = genericObject(new[] { package }, "IncidentCustomFields");
            }
            catch (Exception ex)
            {
                
                WorkspaceAddIn.InfoLog("Exception in Setting Custom Fields: " + ex.Message);
            }
        }*/

        /// <summary>
        /// Add all Incident Vin info in _incidentVINObjects global variable
        /// </summary>
        /// <param name="incVinID">Incidenty VIN record ID</param>
        /// <param name="startDate">warranty start date</param>
        /// <param name="endDate">Warranty end date</param>
        /// <param name="coveredFlag"> Flag whether warranty covered or not</param>
        /// <param name="jSonResp">json response in case of multi option group available for warranty</param>
        /// <param name="optionGrpNo">Option Group Seq No</param>
        /// <returns></returns>
        public BatchRequestItem addIncidentVINRecord(int incVinID, Dictionary<string, string> singleOptionGroup,  string jSonResp)
        {
            try
            {
                GenericObject genObj = new GenericObject
                {
                    ObjectType = new RNObjectType
                    {
                        Namespace = "CO",
                        TypeName = "Incident_VIN"
                    }
                };
                genObj.ID = new ID
                {
                    id = incVinID,
                    idSpecified = true
                };

                List<GenericField> gfs = new List<GenericField>();
                if (jSonResp.Trim() != "")
                {
                    gfs.Add(createGenericField("multi_option_group", createStringDataValue(jSonResp), DataTypeEnum.STRING));
                }
                if (singleOptionGroup != null)
                {
                    if (singleOptionGroup.ContainsKey("under_warranty") && singleOptionGroup["under_warranty"] != "")
                    {
                        Boolean cFlag = (singleOptionGroup["under_warranty"] == "1") ? true : false;
                        gfs.Add(createGenericField("under_warranty", createBooleanDataValue(cFlag), DataTypeEnum.BOOLEAN));
                    }
                    if (singleOptionGroup.ContainsKey("warranty_end_date") && singleOptionGroup["warranty_end_date"] != "")
                    {
                        gfs.Add(createGenericField("warranty_end_date", createDateDataValue(singleOptionGroup["warranty_end_date"]), DataTypeEnum.DATE));
                    }
                    if (singleOptionGroup.ContainsKey("warranty_start_date") && singleOptionGroup["warranty_start_date"] != "")
                    {
                        gfs.Add(createGenericField("warranty_start_date", createDateDataValue(singleOptionGroup["warranty_start_date"]), DataTypeEnum.DATE));
                    }
                    if (singleOptionGroup.ContainsKey("optiongroup_seqno") && singleOptionGroup["optiongroup_seqno"] != "")
                    {
                        gfs.Add(createGenericField("optiongroup_seqno", createStringDataValue(singleOptionGroup["optiongroup_seqno"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("causal_part_nmbr_bom_pn") && singleOptionGroup["causal_part_nmbr_bom_pn"] != "")
                    {
                        gfs.Add(createGenericField("causal_part_nmbr_bom_pn", createStringDataValue(singleOptionGroup["causal_part_nmbr_bom_pn"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("causal_part_desc_bom_pn") && singleOptionGroup["causal_part_desc_bom_pn"] != "")
                    {
                        gfs.Add(createGenericField("causal_part_desc_bom_pn", createStringDataValue(singleOptionGroup["causal_part_desc_bom_pn"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("coverage_name") && singleOptionGroup["coverage_name"] != "")
                    {
                        gfs.Add(createGenericField("coverage_name", createStringDataValue(singleOptionGroup["coverage_name"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("coverage_desc") && singleOptionGroup["coverage_desc"] != "")
                    {
                        gfs.Add(createGenericField("coverage_desc", createStringDataValue(singleOptionGroup["coverage_desc"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("s_policy_name") && singleOptionGroup["s_policy_name"] != "")
                    {
                        gfs.Add(createGenericField("s_policy_name", createStringDataValue(singleOptionGroup["s_policy_name"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("s_policy_desc") && singleOptionGroup["s_policy_desc"] != "")
                    {
                        gfs.Add(createGenericField("s_policy_desc", createStringDataValue(singleOptionGroup["s_policy_desc"]), DataTypeEnum.STRING));
                    }
                    if (singleOptionGroup.ContainsKey("supplier_from_webservice") && singleOptionGroup["supplier_from_webservice"] != "")
                    {
                        gfs.Add(createGenericField("supplier_from_webservice", createNamedIDDataValue(Convert.ToInt32(singleOptionGroup["supplier_from_webservice"])), DataTypeEnum.NAMED_ID));
                    }
                    if (singleOptionGroup.ContainsKey("EWR_Xref_Id") && singleOptionGroup["EWR_Xref_Id"] != "")
                    {
                        gfs.Add(createGenericField("EWR_Xref_Id", createNamedIDDataValue(Convert.ToInt32(singleOptionGroup["EWR_Xref_Id"])), DataTypeEnum.NAMED_ID));
                    }
                }
                genObj.GenericFields = gfs.ToArray();

                _incidentVINObjects.Add(genObj);

                // callBatchJob(getCreateMsg(createObject));
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in creating addIncidentVINRecord records: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Batch Operation to create Incident_VIN records 
        /// </summary>
        /// <param name="busIDs">List of Bus Ids</param>
        /// <param name="internalIncID">Internal Incident ID</param>
        /// <returns></returns>
        public BatchRequestItem createIncidentVIN(List<int> busIDs, int internalIncID)
        {
            try
            {
                List<RNObject> createObject = new List<RNObject>();
                foreach (int busID in busIDs)
                {
                    GenericObject genObj = new GenericObject
                    {
                        ObjectType = new RNObjectType
                        {
                            Namespace = "CO",
                            TypeName = "Incident_VIN"
                        }
                    };

                    List<GenericField> gfs = new List<GenericField>();
                    gfs.Add(createGenericField("Bus", createNamedIDDataValue(busID), DataTypeEnum.NAMED_ID));
                    gfs.Add(createGenericField("Incident", createNamedIDDataValue(internalIncID), DataTypeEnum.NAMED_ID));
                    genObj.GenericFields = gfs.ToArray();

                    createObject.Add(genObj);
                }
                //Don't suppress CPM ..
                CreateMsg createMsg = new CreateMsg();
                createMsg.RNObjects = createObject.ToArray();

                callBatchJob(createMsg);
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in batch job to create Incident_VIN records: " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// function to call Update Incident Vin Batch records
        /// </summary>
        public void updateIncidentVinRecords()
        {
            if (_incidentVINObjects != null && _incidentVINObjects.Count > 0)
            {
                callBatchJob(getUpdateMsg(_incidentVINObjects));
            }
        }

        /// <summary>
        /// Get All internal incident associated with Reporting Incident
        /// </summary>
        /// <param name="reportingIncidentID"></param>
        /// <returns></returns>
        public string[] GetAllInternalIncident(int reportingIncidentID)
        {
            try
            {
                string query = "select I.ID from Incident I where I.CustomFields.CO.reporting_incident = " +
                                reportingIncidentID;

                string[] resultset = GetRNData("Get Internal Incident", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset;
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in getting all Internal Incident " + ex.Message);
            }
            return null;
        }

        /// <summary>
        /// Create or get fsar sales release record
        /// </summary>
        /// <param name="oldFSAR">Previous FSAR value</param>
        /// <param name="newFSAR">New FSAR value</param>
        /// <param name="orgID">Incident Org ID</param>
        /// <returns>FSAR_SalesRelease record ID</returns>
        /*public int CreateFsarSalesRelease(string oldFSAR, string newFSAR, int orgID)
        {
            try
            {
                GenericObject fsarSRObj = new GenericObject
                {
                    ObjectType = new RNObjectType
                    {
                        Namespace = "CO",
                        TypeName = "FSAR_SalesRelease"
                    }
                };
                List<GenericField> gfs = new List<GenericField>();
                if(newFSAR == string.Empty)
                {
                    gfs.Add(createGenericField("FSAR_ID", null, DataTypeEnum.NAMED_ID));
                }
                else
                {
                    gfs.Add(createGenericField("FSAR_ID", createNamedIDDataValue(Convert.ToInt32(newFSAR)), DataTypeEnum.NAMED_ID));
                }
                gfs.Add(createGenericField("Organization_ID", createNamedIDDataValue(orgID), DataTypeEnum.NAMED_ID));
                fsarSRObj.GenericFields = gfs.ToArray();

                //If olf FSAR is not ""
                if (oldFSAR !="")
                {
                    string fsarSRRecord = checkIfFsarSrExistForFSAR(Convert.ToInt32(oldFSAR), orgID);
                    //If FSAR_SR record exist then update it with new FSAR
                    if (fsarSRRecord != null)
                    {
                        fsarSRObj.ID = new ID
                        {
                            id = Convert.ToInt32(fsarSRRecord),
                            idSpecified = true
                        };
                        ClientInfoHeader updateHDR = new ClientInfoHeader { AppID = "Update FSAR_SalesRelease" };
                        _rightNowClient.Update(updateHDR, new RNObject[] { fsarSRObj }, new UpdateProcessingOptions { SuppressExternalEvents=false, SuppressRules= false});
                        return Convert.ToInt32(fsarSRRecord);
                    }
                }
                //Else create a new FSAR_SR record by passing org ID and FSAR ID
                ClientInfoHeader createHDR = new ClientInfoHeader { AppID = "Create FSAR_SalesRelease" };
                RNObject[] rnObject = _rightNowClient.Create(createHDR,
                    new RNObject[] { fsarSRObj },
                    new CreateProcessingOptions
                    {
                        SuppressExternalEvents = false,
                        SuppressRules = false
                    });

                if (rnObject != null)
                {
                    return Convert.ToInt32(rnObject[0].ID.id);
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in fsar sales release Create: " + ex.Message);
            }
            return 0;
        }*/

        /// <summary>
        /// Check if fsar sales release exist for the FSAR
        /// </summary>
        /// <param name="fsarID"></param>
        /// <returns></returns>
        /*public string checkIfFsarSrExistForFSAR(int fsarID, int orgID)
        {
            try
            {
                string query = "select F.ID from CO.FSAR_SalesRelease F where F.FSAR_ID=" + fsarID +
                               " AND F.Organization_ID= " + orgID; 
                string[] resultset = GetRNData("Check FsarSr", query);
                if (resultset != null && resultset.Length > 0)
                {
                    return resultset[0];
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in Checking exisitng fsar sales release: " + ex.Message);
            }
            return null;
        }*/

        /// <summary>
        /// Update Internal Incidents with FSAR ID
        /// </summary>
        /// <param name="incidentIDs"></param>
        /// <param name="fsarID"></param>
        public void UpdateInternalIncident(List<string> incidentIDs, string fsarID)
        {
            List<Incident> internalIncidentList = new List<Incident>();
            foreach (string incID in incidentIDs)
            {
                Incident incObj = new Incident();
                incObj.ID = new ID
                {
                    id = Convert.ToInt32(incID),
                    idSpecified = true
                };
                List<GenericField> customFields = new List<GenericField>();
                //If FSAR id is empty then clear it from Intrenal incident too 
                DataValue fsarData = (fsarID == string.Empty) ? null : createNamedIDDataValue(Convert.ToInt32(fsarID));

                customFields.Add(createGenericField("FSAR", fsarData, DataTypeEnum.NAMED_ID));
                GenericObject customFieldobj = genericObject(customFields.ToArray(), "IncidentCustomFieldsc");
                GenericField package = createGenericField("CO", createObjDataValue(customFieldobj), DataTypeEnum.OBJECT);

                incObj.CustomFields = genericObject(new[] { package }, "IncidentCustomFields");
                internalIncidentList.Add(incObj);
            }
            //Create updateMsg
            UpdateMsg updateMsg = new UpdateMsg();
            UpdateProcessingOptions updateProcessingOption = new UpdateProcessingOptions
            {
                SuppressExternalEvents = false,
                SuppressRules = false
            };
            updateMsg.ProcessingOptions = updateProcessingOption;
            updateMsg.RNObjects = internalIncidentList.ToArray();

            callBatchJob(updateMsg);
        }

        public void UpdateInternalIncidentForSupplier(int internalIncidentID, bool value, string policy)
        {
            Incident incObj = new Incident();
            incObj.ID = new ID
            {
                id = Convert.ToInt32(internalIncidentID),
                idSpecified = true
            };
            List<GenericField> customFields = new List<GenericField>();
            customFields.Add(createGenericField("supplier_recovery", createBooleanDataValue(value), DataTypeEnum.BOOLEAN));
            customFields.Add(createGenericField("supplier_simple_policy", createStringDataValue(policy), DataTypeEnum.STRING));
            GenericObject customFieldobj = genericObject(customFields.ToArray(), "IncidentCustomFieldsc");
            GenericField package = createGenericField("CO", createObjDataValue(customFieldobj), DataTypeEnum.OBJECT);

            incObj.CustomFields = genericObject(new[] { package }, "IncidentCustomFields");
         
            UpdateProcessingOptions updateProcessingOption = new UpdateProcessingOptions
            {
                SuppressExternalEvents = false,
                SuppressRules = false
            };
            _rightNowClient.Update(new ClientInfoHeader { AppID = "Update" }, new RNObject[] { incObj }, updateProcessingOption);

        }


        /// <summary>
        /// auto generate sClaim
        /// </summary>
        /// <param name="incidentID">claim ID</param>
        /// <returns> </returns>
        public void CreatesClaim(int incidentID)
        {
            try
            {
                string existingsClaimID = GetsClaimID(incidentID);
                if (existingsClaimID != null)
                {
                    return;
                }

                GenericObject genObj = new GenericObject
                {
                    ObjectType = new RNObjectType
                    {
                        Namespace = "CO",
                        TypeName = "sClaim"
                    }
                };
                List<GenericField> gfs = new List<GenericField>();
                gfs.Add(createGenericField("Incident", createNamedIDDataValue(Convert.ToInt32(incidentID)), DataTypeEnum.NAMED_ID));
                genObj.GenericFields = gfs.ToArray();

                ClientInfoHeader hdr = new ClientInfoHeader() { AppID = "sClaim Creation" };
                _rightNowClient.Create(hdr, new RNObject[] { genObj }, new CreateProcessingOptions { SuppressExternalEvents = false, SuppressRules = false });
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Excpetion in sClaim Create: " + ex.Message);
            }
            return;
        }

        /// <summary>
        /// Get/check is sCLaim exist for currentyly opened claim
        /// </summary>
        /// <param name="incID">claim id</param>
        /// <returns> string </returns>
        public string GetsClaimID(int incID)
        {
            string query = "SELECT ID FROM CO.sClaim WHERE Incident =" + incID;
            string[] resultSet = GetRNData(" Get sClaim ID", query);
            if (resultSet != null && resultSet.Length > 0)
            {
                return resultSet[0];
            }
            return null;
        }

        /// <summary>
        /// Function to Create\Update Supplier Info Record
        /// </summary>
        /// <param name="incidentID"></param>
        /// <param name="description"></param>
        /// <param name="templateName"></param>
        /// <param name="partsReimbPerc"></param>
        /// <param name="partsMaxAmount"></param>
        /// <param name="laborReimbPerc"></param>
        /// <param name="laborMaxAmount"></param>
        /// <param name="otherReimbPerc"></param>
        /// <param name="otherMaxAmount"></param>
        public void CreateSupplierInfo(int incidentID, string description, string templateName,
                                      string partsReimbPerc, string partsMaxAmount, string laborReimbPerc,
                                      string laborMaxAmount, string otherReimbPerc, string otherMaxAmount)
        {
            try
            {
                string existingSupplierInfoID = checkIfSupplierInfoExist(incidentID);

                GenericObject genObj = new GenericObject
                {
                    ObjectType = new RNObjectType
                    {
                        Namespace = "CO",
                        TypeName = "SupplierInfo"
                    }
                };

                List<GenericField> gfs = new List<GenericField>();
                gfs.Add(createGenericField("description", createStringDataValue(description), DataTypeEnum.STRING));
                gfs.Add(createGenericField("template_name", createStringDataValue(templateName), DataTypeEnum.STRING));
                gfs.Add(createGenericField("Incident", createNamedIDDataValue(incidentID), DataTypeEnum.NAMED_ID));

                if (partsReimbPerc.Trim() != "")
                    gfs.Add(createGenericField("parts_reimb_perc", createStringDataValue(partsReimbPerc), DataTypeEnum.STRING));
                if (partsMaxAmount.Trim() != "")
                    gfs.Add(createGenericField("parts_max_amount", createStringDataValue(partsMaxAmount), DataTypeEnum.STRING));
                if (laborReimbPerc.Trim() != "")
                    gfs.Add(createGenericField("labor_reimb_perc", createStringDataValue(laborReimbPerc), DataTypeEnum.STRING));
                if (laborMaxAmount.Trim() != "")
                    gfs.Add(createGenericField("labor_max_amount", createStringDataValue(laborMaxAmount), DataTypeEnum.STRING));
                if (otherReimbPerc.Trim() != "")
                    gfs.Add(createGenericField("other_reimb_perc", createStringDataValue(otherReimbPerc), DataTypeEnum.STRING));
                if (otherMaxAmount.Trim() != "")
                    gfs.Add(createGenericField("other_max_amount", createStringDataValue(otherMaxAmount), DataTypeEnum.STRING));

                genObj.GenericFields = gfs.ToArray();
                ClientInfoHeader hdr = new ClientInfoHeader() { AppID = "Operation on Supplier Info Record" };
                //If there is a supplier info record mapped with current Incident then update supplier info else create it
                if (existingSupplierInfoID != null)
                {
                    genObj.ID = new ID
                    {
                        id = Convert.ToInt32(existingSupplierInfoID),
                        idSpecified = true
                    };
                    _rightNowClient.Update(hdr, new RNObject[] { genObj }, new UpdateProcessingOptions { SuppressExternalEvents = false, SuppressRules = false });
                }
                else
                {
                    _rightNowClient.Create(hdr, new RNObject[] { genObj }, new CreateProcessingOptions { SuppressExternalEvents = false, SuppressRules = false });
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Excpetion in Supplier Info Create: " + ex.Message);
            }
            return;
        }

        /// <summary>
        /// Check if Supplier Info Already Exist
        /// </summary>
        /// <param name="incidentID"> Current Incident ID</param>
        /// <returns>Supplier info record ID</returns>
        public string checkIfSupplierInfoExist(int incidentID)
        {
            try
            {
                string query = "Select S.ID from CO.SupplierInfo S where S.Incident = " + incidentID;
                string[] resultSet = GetRNData("Get SupplierInfo record", query);
                if (resultSet != null && resultSet.Length > 0)
                {
                    return resultSet[0];
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in check for supplier info " + ex.Message);
            }
            return null;
        }


        #region Miscellaneous Operations
        /// <summary>
        /// Perform Batch operation
        /// </summary>
        /// <param name="msg">BatchRequestItem Item</param>
        public void callBatchJob(Object msg)
        {
            try
            {
                /*** Form BatchRequestItem structure ********************/

                BatchRequestItem[] requestItems = new BatchRequestItem[1];

                BatchRequestItem requestItem = new BatchRequestItem();

                requestItem.Item = msg;

                requestItems[0] = requestItem;
                requestItems[0].CommitAfter = true;
                requestItems[0].CommitAfterSpecified = true;
                /*********************************************************/

                ClientInfoHeader clientInfoHeader = new ClientInfoHeader();
                clientInfoHeader.AppID = "Batcher";

                BatchResponseItem[] batchRes = _rightNowClient.Batch(clientInfoHeader, requestItems);
                //If response type is RequestErrorFaultType then show the error msg 
                if (batchRes[0].Item.GetType().Name == "RequestErrorFaultType")
                {                    
                    RequestErrorFaultType requestErrorFault = (RequestErrorFaultType)batchRes[0].Item;
                    WorkspaceAddIn.InfoLog("There is an error with batch job :: " + requestErrorFault.exceptionMessage);
                }
            }
            catch (FaultException ex)
            {                
                WorkspaceAddIn.InfoLog(ex.Message);
                return;
            }
            catch (Exception ex)
            {                
                WorkspaceAddIn.InfoLog(ex.Message);
                return;
            }
        }

        /// <summary>
        /// To create Update Message for Batch
        /// </summary>
        /// <param name="coList"></param>
        /// <returns></returns>
        private UpdateMsg getUpdateMsg(List<RNObject> coList)
        {
            UpdateMsg updateMsg = new UpdateMsg();
            UpdateProcessingOptions updateProcessingOptions = new UpdateProcessingOptions();
            updateProcessingOptions.SuppressExternalEvents = true;
            updateProcessingOptions.SuppressRules = true;
            updateMsg.ProcessingOptions = updateProcessingOptions;

            updateMsg.RNObjects = coList.ToArray();

            return updateMsg;
        }

        /// <summary>
        /// Create CreateMsg object
        /// </summary>
        /// <param name="coList">RNObject List</param>
        /// <returns> CreateMsg</returns>
        private CreateMsg getCreateMsg(List<RNObject> coList)
        {
            CreateMsg createMsg = new CreateMsg();
            CreateProcessingOptions createProcessingOptions = new CreateProcessingOptions();
            createProcessingOptions.SuppressExternalEvents = true;
            createProcessingOptions.SuppressRules = true;
            createMsg.ProcessingOptions = createProcessingOptions;

            createMsg.RNObjects = coList.ToArray();

            return createMsg;
        }

        /// <summary>
        /// Create DestroyMsg object
        /// </summary>
        /// <param name="coList">RNObject List</param>
        /// <returns> DestroyMsg</returns>
        private DestroyMsg getDestroyMsg(List<RNObject> coList)
        {
            DestroyMsg deleteMsg = new DestroyMsg();
            DestroyProcessingOptions deleteProcessingOptions = new DestroyProcessingOptions();
            deleteProcessingOptions.SuppressExternalEvents = true;
            deleteProcessingOptions.SuppressRules = true;
            deleteMsg.ProcessingOptions = deleteProcessingOptions;

            deleteMsg.RNObjects = coList.ToArray();

            return deleteMsg;
        }

        /// <summary>
        /// Create Boolean type data value
        /// </summary>
        /// <param name="val"></param>
        /// <returns> DataValue</returns>
        private DataValue createBooleanDataValue(Boolean val)
        {
            DataValue dv = new DataValue();
            dv.Items = new Object[] { val };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.BooleanValue };

            return dv;
        }

        /// <summary>
        /// Create string type data value
        /// </summary>
        /// <param name="val"></param>
        /// <returns> DataValue</returns>
        private DataValue createStringDataValue(string val)
        {
            DataValue dv = new DataValue();
            dv.Items = new Object[] { val };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.StringValue };  //Change this to the type of field
            return dv;
        }

        /// <summary>
        /// Create DataValue object
        /// </summary>
        /// <param name="val">DataValue Item </param>
        /// <returns> DataValue</returns>
        private DataValue createObjDataValue(object val)
        {
            DataValue dv = new DataValue();
            dv.Items = new Object[] { val };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.ObjectValue };  //Change this to the type of field
            return dv;
        }

        /// <summary>
        /// 
        /// Create Date type data value
        /// </summary>
        /// <param name="val"></param>
        /// <returns> DataValue</returns>
        private DataValue createDateDataValue(string val)
        {
            //DateTime date = (Convert.ToDateTime("24-FEB-2019 00:00:00"));//Have hard coded for testing....DO NOT REMOVE 
            DateTime date = (Convert.ToDateTime(val));
            DataValue dv = new DataValue();
            dv.Items = new Object[] { date };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.DateValue };  //Change this to the type of field
            return dv;
        }

        /// <summary>
        /// Create GenericField object
        /// </summary>
        /// <param name="name">Name Of Generic Field</param>
        /// <param name="dataValue">Vlaue of generic field</param>
        /// <param name="type">Type of generic field</param>
        /// <returns> GenericField</returns>
        private GenericField createGenericField(string name, DataValue dataValue, DataTypeEnum type)
        {
            GenericField genericField = new GenericField();

            genericField.dataType = type;
            genericField.dataTypeSpecified = true;
            genericField.name = name;
            genericField.DataValue = dataValue;
            return genericField;
        }
        /// <summary>
        /// Create Named ID type data value
        /// </summary>
        /// <param name="idVal"></param>
        /// <returns> DataValue</returns>
        private DataValue createNamedIDDataValue(long idVal)
        {
            ID id = new ID();
            id.id = idVal;
            id.idSpecified = true;

            NamedID namedID = new NamedID();
            namedID.ID = id;

            DataValue dv = new DataValue();
            dv.Items = new Object[] { namedID };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.NamedIDValue };

            return dv;
        }
        /// <summary>
        /// Create Named ID type data value for Name
        /// </summary>
        /// <param name="name"></param>
        /// <returns> DataValue</returns>
        private DataValue createNamedIDDataValueForName(string name)
        {
            NamedID namedID = new NamedID();
            namedID.Name = name;

            DataValue dv = new DataValue();
            dv.Items = new Object[] { namedID };
            dv.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.NamedIDValue };

            return dv;
        }
        /// <summary>
        /// Create Generic Object type data value
        /// </summary>
        /// <param name="gF">Array of Generic Field</param>
        /// <param name="typeName">RNObjectType Type name</param>
        /// <returns> GenericObject</returns>
        private GenericObject genericObject(GenericField[] gF, string typeName)
        {
            RNObjectType rnObjType = new RNObjectType();
            rnObjType.TypeName = typeName;

            GenericObject gObj = new GenericObject();
            gObj.GenericFields = gF;
            gObj.ObjectType = rnObjType;

            return gObj;
        }
        #endregion
    }
}
