using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Reported_Incident_Automation.SingleWarrantyReqParam;
using System.Web.Script.Serialization;
using RightNow.AddIns.AddInViews;

namespace Reported_Incident_Automation
{
    class SingleWarrantyModel
    {
        public static string _odometer;
        public static string _causalPart;
        public static string _failureDate;
        public static string _vin;
        public static string _srNum;
        public static int _incVinID;
        public static string _curlURL;
        public static string _xmlnsURL;
        public static string _headerURL;
        public static string _responsibility;
        public static string _respApplication;
        public static string _securityGroup;
        public static string _nLSLanguage;
        public static string _orgID;
        public static RightNowConnectService _rnConnectService;
        IIncident _incidentRecord;

        public SingleWarrantyModel()
        {
            _rnConnectService = RightNowConnectService.GetService();
            string singleWarrantyConfigValue = _rnConnectService.GetConfigValue("CUSTOM_CFG_BOM_QUERY");
            if (singleWarrantyConfigValue != null)
            {

                var s = new JavaScriptSerializer();

                var configVerb = s.Deserialize<ConfigVerbs.RootObject>(singleWarrantyConfigValue);
                _curlURL = configVerb.URL;
                _headerURL = configVerb.xmlns;
                _xmlnsURL = configVerb.RESTHeader.xmlns;
                _respApplication = configVerb.RESTHeader.RespApplication;
                _responsibility = configVerb.RESTHeader.Responsibility;
                _securityGroup = configVerb.RESTHeader.SecurityGroup;
                _nLSLanguage = configVerb.RESTHeader.NLSLanguage;
                _orgID = configVerb.RESTHeader.Org_Id;
            }
        }
        /// <summary>
        /// Method to perform warranty check for creating "WARRANTY" type work order using old BOM query. 
        /// </summary>
        public void WarrantyCheck(IIncident incidentRecord)
        {
            _rnConnectService = RightNowConnectService.GetService();
            _rnConnectService._incidentVINObjects.Clear();//Clear the _incidentVINObjects variable that holds all incident_vin record to be updated with response 
            _incidentRecord = incidentRecord;

            //Get basic field that need to be pass in webservce call, 
            //Null validation of these fields are handle by workspace rules
            _odometer = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "odometer_reading", _incidentRecord);
            _causalPart = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "causal_part_nmbr", _incidentRecord);
            string failureDateInSTring = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "failure_date", _incidentRecord);
            _failureDate = Convert.ToDateTime(failureDateInSTring).ToString("dd-MMM-yyyy");
            string[] busInfo = _rnConnectService.getBusInfo(_incidentRecord.ID);
            if (busInfo != null && busInfo.Length > 0)
            {
                if (busInfo.Length > 1)
                {
                    WorkspaceAddIn.InfoLog("It seems multi VIN are mapped with Reporting Incident, warrant type work order" +
                                    " works only for individual VIN");
                    return;
                }
                else
                {
                    _vin = busInfo[0].Split('~')[0];
                    _srNum = busInfo[0].Split('~')[1];
                    _incVinID = Convert.ToInt32(busInfo[0].Split('~')[2]);

                    //If all required info is valid then form jSon request parameter
                    var content = GetWarrantyReqParam();
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
                        ExtractResponse(jsonResponse);
                        _rnConnectService.updateIncidentVinRecords();
                    }
                }
            }
            else
            {
                WorkspaceAddIn.InfoLog("No Bus info found, please map a bus to reporting Incident and then click check warranty button");
                return;
            }
            return;
        }
        /// <summary>
        /// Funtion to form Warrant Req Param Structure
        /// </summary>
        public static ContentObject GetWarrantyReqParam()
        {
            var content = new ContentObject
            {
                ISPARTWARRANTABLE_Input = new ISPARTWARRANTABLEInput
                {
                    @xmlns = _xmlnsURL,
                    RESTHeader = new RESTHeader
                    {
                        @xmlns = _headerURL,
                        Responsibility = _responsibility,
                        RespApplication = _respApplication,
                        SecurityGroup = _securityGroup,
                        NLSLanguage = _nLSLanguage,
                        Org_id = _orgID
                    },
                    InputParameters = new InputParameters
                    {
                        P_SR = _srNum,
                        P_VIN = _vin,
                        P_PART = _causalPart,
                        P_ODOMETER = _odometer,
                        P_FAILDATE = _failureDate
                        /*P_SR = "SR-2000",
                        P_VIN = "5FYC8FB03GB049200",
                        P_PART = "6336848",
                        P_ODOMETER = "1000",
                        P_FAILDATE = "21-MAR-2017"*/
                    }
                }
            };
            return content;
        }
        /// <summary>
        /// Funtion to handle ebs webservice response
        /// </summary>
        /// <param name="respJson">response in jSON string</param>
        public void ExtractResponse(string jsonResp)
        {
            //save webservice response
            // RightNowConnectService.GetService().setIncidentField("CO", "WebServiceResponse", jsonResp, _incidentRecord);
            Dictionary<string, string> incidentVinInfo = new Dictionary<string, string>();
            Dictionary<string, object> singleOptionGroup;
            jsonResp = jsonResp.Replace("@xmlns:xsi", "@xmlns_xsi");//formating json string
            jsonResp = jsonResp.Replace("@xsi:nil", "@xsi_nil");//formating json string
            jsonResp = jsonResp.Replace("PKG_V2-24", "PKG_V2_24");//formating json string
            
            //converting json string to Dictionary<string, object>
           Dictionary <string, object> data = (Dictionary<string, object>)WebServiceRequest.JsonDeSerialize(jsonResp);
            Dictionary<string, object> output = (Dictionary<string, object>)data["OutputParameters"];
            Dictionary<string, object> param = (Dictionary<string, object>)output["CIN_BOM_QUERY_PKG_V2_24ISPARTWA"];

            string combo = "";
            string busModel = "";
            string vin = "";
            string[] ewrInfo;

            string[] busInfo = _rnConnectService.getBusInfoIV(_incVinID);

            //Check if multi option group is retured, if so then save jSon response
            if (IsArray(param["CIN_BOM_QUERY_PKG_V2_24ISPARTWA_ITEM"]))
            {
                object[] optionItems = (object[])param["CIN_BOM_QUERY_PKG_V2_24ISPARTWA_ITEM"];

                if (optionItems.Length == 1)//if one elemnet in an array that mean too single option group
                {                 
                    singleOptionGroup = (Dictionary<string, object>)optionItems[0];
                }
                else
                {
                    //Get part desc from first item, as it will same across all item of multioption grp
                    Dictionary<string, object> firstItem = (Dictionary<string, object>)optionItems[0];
                    RightNowConnectService.GetService().setIncidentField("CO", "causal_part_desc", firstItem["PART_DESC"].ToString(), _incidentRecord);

                    //Include the vendor id (org) and EWR_Xref_Id in multi option grp object[], so it can be used in other add-in logic
                    foreach (object option in optionItems)
                    {
                        Dictionary<string, object> response = (Dictionary<string, object>)option;
                        if (busInfo != null && busInfo.Length > 0)
                        {
                            vin = busInfo[0].Split('~')[0];
                            busModel = vin.Substring(4, 1);
                            combo = busModel + "-" + response["OPTIONGROUP_SEQNO"].ToString().Trim();

                            ewrInfo = _rnConnectService.getEWRID(combo);
                            if (ewrInfo != null && ewrInfo.Length > 0)
                            {
                                response.Add("EWR_Xref_Id", ewrInfo[0].Split('~')[0]);// add  EWR_Xref_Id in response list for multi option grp                        
                            }
                        }
                    }
                    string optionGrpJson = WebServiceRequest.JsonSerialize(output["CIN_BOM_QUERY_PKG_V2_24ISPARTWA"]);
                    _rnConnectService.addIncidentVINRecord(_incVinID, null, optionGrpJson);
                    return;
                }
            }
            //If 1 item is retured then set individual field of incident_vin record
            else
            {
                singleOptionGroup = (Dictionary<string, object>)param["CIN_BOM_QUERY_PKG_V2_24ISPARTWA_ITEM"];
            }
            if (singleOptionGroup != null)
            {
                
                RightNowConnectService.GetService().setIncidentField("CO", "causal_part_desc", singleOptionGroup["PART_DESC"].ToString(), _incidentRecord);
                

                if (singleOptionGroup["VENDOR_ID"].ToString().Trim() != "")
                {
                    //Get OOTB ORG ID from EBS ORG ID
                    string orgID = _rnConnectService.GetOrgID(Convert.ToInt32(singleOptionGroup["VENDOR_ID"].ToString()));
                    //RightNowConnectService.GetService().setIncidentField("CO", "supplier_from_webservice", orgID, _incidentRecord);
                    incidentVinInfo.Add("supplier_from_webservice", orgID);
                }
                if (singleOptionGroup["DESCRIPTION"].ToString().Trim() == "Warranty Start Date and Date From Plant are blank for the VIN")
                {
                    WorkspaceAddIn.InfoLog(singleOptionGroup["DESCRIPTION"].ToString()); 
                }

                incidentVinInfo.Add("under_warranty", singleOptionGroup["ISCOVERED"].ToString().Trim());
                incidentVinInfo.Add("causal_part_nmbr_bom_pn", singleOptionGroup["PART_NUMBER"].ToString().Trim());
                incidentVinInfo.Add("causal_part_desc_bom_pn", singleOptionGroup["DESCRIPTION"].ToString().Trim());                
                incidentVinInfo.Add("coverage_name", singleOptionGroup["COVERAGE_NAME"].ToString().Trim());
                incidentVinInfo.Add("coverage_desc", singleOptionGroup["COVERAGE_DESC"].ToString().Trim());
                incidentVinInfo.Add("optiongroup_seqno", singleOptionGroup["OPTIONGROUP_SEQNO"].ToString().Trim());
                incidentVinInfo.Add("s_policy_name", singleOptionGroup["SPOLICY_NAME"].ToString().Trim());
                incidentVinInfo.Add("s_policy_desc", singleOptionGroup["SPOLICY_DESC"].ToString().Trim());
                //_rnConnectService.addIncidentVINRecord(_incVinID, "", "", singleOptionGroup["ISCOVERED"].ToString(), "",
                //                       singleOptionGroup["OPTIONGROUP_SEQNO"].ToString());
                //Logic to update EWR_Xref_Id field

                if (busInfo != null && busInfo.Length > 0)
                {
                    vin = busInfo[0].Split('~')[0];
                    if (vin != "") { 
                    busModel = vin.Substring(4, 1);
                    combo = busModel + "-" + singleOptionGroup["OPTIONGROUP_SEQNO"].ToString().Trim();

                    ewrInfo = _rnConnectService.getEWRID(combo);
                    if (ewrInfo != null && ewrInfo.Length > 0)
                    {
                        incidentVinInfo.Add("EWR_Xref_Id", ewrInfo[0].Split('~')[0]);
                    }
                   }
                }
                _rnConnectService.addIncidentVINRecord(_incVinID, incidentVinInfo, "");
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
    }
}
