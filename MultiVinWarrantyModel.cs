using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Web.Script.Serialization;
using static Reported_Incident_Automation.MultiVinWarrantyReqParam;
using RightNow.AddIns.AddInViews;

namespace Reported_Incident_Automation
{
    class MultiVinWarrantyModel
    {
        public static string _causalPart;
        public static string _curlURL;
        public static string _xmlnsURL;
        public static string _headerURL;
        public static string _responsibility;
        public static string _respApplication;
        public static string _securityGroup;
        public static string _nLSLanguage;
        public static string _orgID;
        List<string> _allInternalincident = new List<string>();
        public static RightNowConnectService _rnConnectService;
        private IIncident _incidentRecord;

        /// <summary>
        /// Get required details to build WebRequest
        /// </summary>
        public void MultiVinWarrantyCheck(IIncident incidentRecord)
        {
            try
            {
                
                _rnConnectService = RightNowConnectService.GetService();
                _incidentRecord = incidentRecord;
                string multiWarrantyConfigValue = _rnConnectService.GetConfigValue("CUSTOM_CFG_NEW_BOM_QUERY");
                if (multiWarrantyConfigValue != null)
                {
                   
                    var s = new JavaScriptSerializer();

                    var configVerb = s.Deserialize<ConfigVerbs.RootObject>(multiWarrantyConfigValue);
                    _curlURL = configVerb.URL;
                    _headerURL = configVerb.xmlns;
                    _xmlnsURL = configVerb.RESTHeader.xmlns;
                    _respApplication = configVerb.RESTHeader.RespApplication;
                    _responsibility = configVerb.RESTHeader.Responsibility;
                    _securityGroup = configVerb.RESTHeader.SecurityGroup;
                    _nLSLanguage = configVerb.RESTHeader.NLSLanguage;
                    _orgID = configVerb.RESTHeader.Org_Id;
                }

                _rnConnectService._incidentVINObjects.Clear();//Clear the _incidentVINObjects variable that holds all incident_vin record to be updated with response 
                
                _causalPart = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "causal_part_nmbr",incidentRecord);
               
                string[] internalIncInfo = _rnConnectService.GetInternalIncident(incidentRecord.ID);
                if (internalIncInfo != null)
                {
                    _allInternalincident = internalIncInfo.ToList();
                }
                if (_allInternalincident.Count > 0)
                {
                    var tasks = new Task[_allInternalincident.Count];
                    int ii = 0;
                    foreach (string internalIncident in _allInternalincident)
                    {
                        tasks[ii++] = Task.Factory.StartNew(() => RequestPerSR(internalIncident));
                    }
                    Task.WaitAll(tasks);
                    _rnConnectService.updateIncidentVinRecords();//Once all task over, call batch job to update Incident_VIN record
                }
    
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exeption in GetDetails: " + ex.Message);
            }
        }
        /// <summary>
        /// Build Request-Response 
        /// </summary>
        /// <param name="internalIncident">Internal incident info in string separated by "~"</param>
        public void RequestPerSR(string internalIncident)
        {
            try
            {
                //Get VIN and Incident_VIN record ID from Internal incident 
                string[] vinsOfInternalInc = _rnConnectService.GetVins(Convert.ToInt32(internalIncident.Split('~')[0]));
                if (vinsOfInternalInc != null)
                {
                    List<VINREC> vinlist = new List<VINREC>();
                    foreach (string individualVIN in vinsOfInternalInc)
                    {
                        string VinNo = individualVIN.Split('~')[0];//get VIN #
                        VINREC vinRecord = new VINREC { VIN = VinNo };
                        vinlist.Add(vinRecord);
                    }
                    //If all required info is valid then form jSon request parameter
                    var content = GetMultiVinWarrantyReqParam(internalIncident.Split('~')[2], vinlist);
                    var jsonContent = WebServiceRequest.JsonSerialize(content);
                    jsonContent = jsonContent.Replace("xmlns", "@xmlns");
                  
                    //Call webservice 
                    string jsonResponse = WebServiceRequest.Get(_curlURL, jsonContent, "POST");
                    if (jsonResponse == "")
                    {
                        WorkspaceAddIn.InfoLog("Server didn't returned any info");
                        return;
                    }
                    else
                    {
                        ExtractResponse(jsonResponse, vinsOfInternalInc);
                        _rnConnectService.updateIncidentVinRecords();
                    }
                }
            }
            catch (Exception ex)
            {
                WorkspaceAddIn.InfoLog("Exception in WebRequest: " + ex.Message);
            }
        }
        /// <summary>
        /// Funtion to form Warrant Req Param Structure
        /// </summary>
        public RootObject GetMultiVinWarrantyReqParam(string srNum, List<VINREC> vinlist)
        {
            RootObject rootObj = new RootObject
            {
                GETVINWARRANTYPERIODS_Input = new GETVINWARRANTYPERIODSInput
                {
                    xmlns = _xmlnsURL,
                    RESTHeader = new RESTHeader
                    {
                        xmlns = _headerURL,
                        Responsibility = _responsibility,
                        RespApplication = _respApplication,
                        SecurityGroup = _securityGroup,
                        NLSLanguage = _nLSLanguage,
                        Org_Id = _orgID
                    },
                    InputParameters = new InputParameters
                    {
                        P_SR = srNum,
                        P_VIN_TBL = new PVINTBL
                        {
                            VIN_REC = vinlist
                        },
                        P_PART = _causalPart
                    }
                }
            };
            return rootObj;
        }
        /// <summary>
        /// Funtion to handle ebs webservice response
        /// </summary>
        /// <param name="respJson">response in jSON string</param>
        public void ExtractResponse(string jsonResp, string[] vinsOfInternalInc)
        {
            //Extract response
            Dictionary<string, object> data = (Dictionary<string, object>)WebServiceRequest.JsonDeSerialize(jsonResp);
            Dictionary<String, object> outputParameters = (Dictionary<String, object>)data["OutputParameters"];
            Dictionary<String, object> output = (Dictionary<String, object>)outputParameters["CIN_BOM_QUERY_PKG_V2-24GETVINWA"];

            //loop over each vin response                 
            if (IsArray(output["CIN_BOM_QUERY_PKG_V2-24GETVINWA_ITEM"]))
            {
                foreach (object vinItem in (object[])output["CIN_BOM_QUERY_PKG_V2-24GETVINWA_ITEM"])
                {
                    ExtractIncidentVinInfo(vinItem, vinsOfInternalInc);
                }
            }
            else
            {
                ExtractIncidentVinInfo(output["CIN_BOM_QUERY_PKG_V2-24GETVINWA_ITEM"], vinsOfInternalInc);
            }
        }

        /// <summary>
        /// Funtion to check if object is array
        /// </summary>
        /// <param name="bool">response in boolean</param>
        public static bool IsArray(object o)
        {
            return o is Array;
        }
        /// <summary>
        /// Get the Incident Vin ID
        /// </summary>
        /// <param name="vinInfos"></param>
        /// <param name="vin"></param>
        /// <returns></returns>
        private void ExtractIncidentVinInfo(object vinItem, string[] vinsOfInternalInc)
        {
            Dictionary<string, object> singleOptionGroup = null;
            string orgID = "";
            Dictionary<string, object> item = (Dictionary<string, object>)vinItem;
            Dictionary<string, string> incidentVinInfo = new Dictionary<string, string>();
            string combo = "";
            string busModel = "";
            string vin = "";

           
            
            int incidentVinID = getIncidentVinID(vinsOfInternalInc, item["VIN"].ToString());
            if (item["VENDOR_ID"].ToString().Trim() != "")
            {
                //Get OOTB ORG ID from EBS ORG ID
                orgID = _rnConnectService.GetOrgID(Convert.ToInt32(item["VENDOR_ID"].ToString()));
                //RightNowConnectService.GetService().setIncidentField("CO", "supplier_from_webservice", orgID,_incidentRecord);
                incidentVinInfo.Add("supplier_from_webservice", orgID);
            }
            string[] busInfo = _rnConnectService.getBusInfoIV(incidentVinID);
            string[] ewrInfo;
            RightNowConnectService.GetService().setIncidentField("CO", "causal_part_desc", item["PART_DESC"].ToString(), _incidentRecord);

            Dictionary<string, object> vinSubResult = (Dictionary<string, object>)item["VIN_SUB_RESULT"];
            //Check if multi option group is retured, if so then save jSon response
           
            if (IsArray(vinSubResult["VIN_SUB_RESULT_ITEM"]))
            {
               
                object[] optionItems = (object[])vinSubResult["VIN_SUB_RESULT_ITEM"];
                if (optionItems.Length == 1)//if one elemnet in an array that mean too single option group
                {
                    
                    singleOptionGroup = (Dictionary<string, object>)optionItems[0];
                    
                    if (busInfo != null && busInfo.Length > 0)
                    {
                        vin = busInfo[0].Split('~')[0];
                        busModel = vin.Substring(4, 1);
                        combo = busModel + "-" + singleOptionGroup["OPTIONGROUP_SEQNO"].ToString().Trim();
                        
                        ewrInfo = _rnConnectService.getEWRID(combo);
                        if (ewrInfo != null && ewrInfo.Length > 0)
                        {
                            incidentVinInfo.Add("EWR_Xref_Id", ewrInfo[0].Split('~')[0]);
                            
                        }
                        
                    }
                }
                else
                {
                   
                    //Include the vendor id (org) in multi option grp object[], so it can be used in other add-in logic
                    foreach (object option in (object[])((Dictionary<string, object>)((Dictionary<string, object>)vinItem)["VIN_SUB_RESULT"])["VIN_SUB_RESULT_ITEM"])
                    {
                        Dictionary<string, object> response = (Dictionary<string, object>)option;
                        if (busInfo != null && busInfo.Length > 0)
                        {
                            vin = busInfo[0].Split('~')[0];
                            busModel = vin.Substring(4, 1);
                            combo =  busModel + "-" + response["OPTIONGROUP_SEQNO"].ToString().Trim();
                           
                            ewrInfo = _rnConnectService.getEWRID(combo);
                            if (ewrInfo != null && ewrInfo.Length > 0)
                            {
                                incidentVinInfo.Add("EWR_Xref_Id", ewrInfo[0].Split('~')[0]);
                              
                            }
                        }
                        
                        response.Add("VENDOR_ID", orgID);


                    }
                    string optionGrpJson = WebServiceRequest.JsonSerialize(((Dictionary<string, object>)vinItem)["VIN_SUB_RESULT"]);
                    _rnConnectService.addIncidentVINRecord(incidentVinID, null, optionGrpJson);
                }
            }
            //If not in array that means single option group
            else
            {
               
                singleOptionGroup = (Dictionary<String, object>)vinSubResult["VIN_SUB_RESULT_ITEM"];


                if (busInfo != null && busInfo.Length > 0)
                {
                    vin = busInfo[0].Split('~')[0];
                    busModel = vin.Substring(4, 1);
                    combo = busModel + "-" + singleOptionGroup["OPTIONGROUP_SEQNO"].ToString().Trim();
                   
                    ewrInfo = _rnConnectService.getEWRID(combo);
                    if (ewrInfo != null && ewrInfo.Length > 0)
                    {
                        incidentVinInfo.Add("EWR_Xref_Id", ewrInfo[0].Split('~')[0]);
                      
                    }

                }
            }

            if (singleOptionGroup != null)
            {
               
                incidentVinInfo.Add("warranty_start_date", singleOptionGroup["WARRANTY_START_DATE"].ToString().Trim());
                incidentVinInfo.Add("warranty_end_date", singleOptionGroup["WARRANTY_END_DATE"].ToString().Trim());
                incidentVinInfo.Add("under_warranty", singleOptionGroup["ISCOVERED"].ToString().Trim());
                incidentVinInfo.Add("optiongroup_seqno", singleOptionGroup["OPTIONGROUP_SEQNO"].ToString().Trim());
                incidentVinInfo.Add("causal_part_desc_bom_pn", singleOptionGroup["DESCRIPTION"].ToString().Trim());
                incidentVinInfo.Add("causal_part_nmbr_bom_pn", singleOptionGroup["PART_NUMBER"].ToString().Trim());

                /*_rnConnectService.addIncidentVINRecord(incidentVinID, singleOptionGroup["WARRANTY_START_DATE"].ToString(),
                                       singleOptionGroup["WARRANTY_END_DATE"].ToString(),
                                       singleOptionGroup["ISCOVERED"].ToString(), "",
                                       singleOptionGroup["OPTIONGROUP_SEQNO"].ToString());*/
                _rnConnectService.addIncidentVINRecord(incidentVinID, incidentVinInfo, "");
            }
        }
        /// <summary>
        /// Get the Incident Vin ID from Incident_VIN~VIN pair array
        /// </summary>
        /// <param name="vinInfos"></param>
        /// <param name="vin"></param>
        /// <returns></returns>
        private int getIncidentVinID(string[] vinInfos, string vin)
        {
            foreach (string vinInfo in vinInfos)
            {
                if (vinInfo.Split('~')[0] == vin)//0th element has VIN
                {
                    return Convert.ToInt32(vinInfo.Split('~')[1]);//element at 1 is Incident_VIn ID
                }
            }
            return 0;
        }
    }
}
