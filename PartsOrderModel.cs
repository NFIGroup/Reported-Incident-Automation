using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using static Reported_Incident_Automation.PartsOrderReqParam;
using RightNow.AddIns.AddInViews;

namespace Reported_Incident_Automation
{
    class PartsOrderModel
    {
        public string[] _partsInfo;
        public string _curlURL;
        public string _xmlnsURL;
        public string _headerURL;
        public string _responsibility;
        public string _respApplication;
        public string _securityGroup;
        public string _nLSLanguage;
        public string _orgID;
        public RightNowConnectService _rnConnectService;

        public PartsOrderModel()
        {
            _rnConnectService = RightNowConnectService.GetService();
            string partsConfigValue = _rnConnectService.GetConfigValue("CUSTOM_CFG_PARTS_ORDER");
            if(partsConfigValue != null)
            {
                var s = new JavaScriptSerializer();

                var configVerb = s.Deserialize<ConfigVerbs.RootObject>(partsConfigValue);
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
        /// This function does basic validation like if unordered parts beed added to reported Incident, if so then 
        /// Call EBS parts order web-service to pass parts related info and store NF_SALES_ORDER no to each parts record
        /// </summary>
        /// <returns>string content that need to send to web-service</returns>
        public void OrderParts(IIncident incidentRecord)
        {            
            //Get reported Incident Info
            int orderTypeId = Convert.ToInt32(RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "order_type",incidentRecord));
            int billToId = Convert.ToInt32(RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "Bill_to_site",incidentRecord));
            int shipToId = Convert.ToInt32(RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "Ship_to_site",incidentRecord));
            string claimNum = incidentRecord.RefNo;
            string projectNum = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "project_number",incidentRecord);
            string retrofitNum = RightNowConnectService.GetService().getFieldFromIncidentRecord("CO", "retrofit_number",incidentRecord);

            //Get EBS ID from OSvC OOTB ID
            string billToEbsID = _rnConnectService.GetEbsID(billToId);
            string shipToEbsID = _rnConnectService.GetEbsID(shipToId);
            string orgEbsID = _rnConnectService.GetBusOwnerEbsID(Convert.ToInt32(incidentRecord.OrgID));
            string odrTypName = _rnConnectService.GetOrderTypeName(orderTypeId);

            //Get unordered Parts mapped to reported incident
            _partsInfo = _rnConnectService.GetPartsInfo(incidentRecord.ID);
            if (_partsInfo == null || _partsInfo.Length <= 0)
            {
                WorkspaceAddIn.InfoLog("No parts have beed added to order");
                return ;
            }
            else
            {
                //Frame parts order request param structure
                var content = GetPartsOdrReqParam(odrTypName, orgEbsID, shipToEbsID, billToEbsID, claimNum, projectNum, retrofitNum);
                if(content == null)
                {
                    return ;
                }
                else
                {
                    //Convert object to jSon string
                    var jsonContent = WebServiceRequest.JsonSerialize(content);
                    jsonContent = jsonContent.Replace("xmlns", "@xmlns");

                    //Call web-service
                    string jsonResponse = WebServiceRequest.Get(_curlURL, jsonContent, "POST");

                    if (jsonResponse == "")
                    {
                        WorkspaceAddIn.InfoLog("Server didn't returned any info");
                        return ;
                    }
                    else
                    {
                        ExtractResponse(jsonResponse);
                        _rnConnectService.updateIncidentVinRecords();
                    }
                }
            }
        }
        /// <summary>
        /// Funtion to form Parts Order Req Param Structure
        /// </summary>
        public RootObject GetPartsOdrReqParam(string orderTypeName, string bosOwnerOrgEbsID, string shipToEbsID,
                                                string billToEbsID, string claimNum, string projectNumber, string retrofitNumber)
        {
            List<OELineRec> OE_LINE_REC = new List<OELineRec>();
            foreach (string partinfo in _partsInfo)
            {
                string[] info = partinfo.Split('~');
                OELineRec oeLineRec = new OELineRec();
                oeLineRec.ORDERED_ID = info[0];
                if (info[1] == "" || info[1] == null)
                {
                    WorkspaceAddIn.InfoLog("Part Number is missing for part id " + info[0]);
                    return null;
                }
                oeLineRec.ORDERED_ITEM = info[1];
                if (info[2] == "" || info[2] == null)
                {
                    WorkspaceAddIn.InfoLog("Part quantity is missing for part id " + info[0]);
                    return null;
                }
                oeLineRec.ORDERED_QUANTITY = info[2];

                oeLineRec.SHIP_SET = info[4];
                oeLineRec.SOURCE_TYPE = info[3];
                OE_LINE_REC.Add(oeLineRec);
            }
            var content = new RootObject
            {
                CREATE_A_SALES_ORDER_Input = new CreateASalesOrderInput
                {
                    @xmlns = _headerURL,
                    RESTHeader = new RESTHeader
                    {
                        @xmlns = _xmlnsURL,
                        Responsibility = _responsibility,
                        RespApplication = _respApplication,
                        SecurityGroup = _securityGroup,
                        NLSLanguage = _nLSLanguage,
                        Org_Id = _orgID
                    },
                    InputParameters = new InputParameters
                    {
                        P_OE_HEADER_REC = new POeHeaderRec
                        {
                            ORDER_TYPE = orderTypeName,
                            CUSTOMER_ID = bosOwnerOrgEbsID,
                            SHIP_TO_ORG_ID = shipToEbsID,
                            INVOICE_TO_ORG_ID = billToEbsID,
                            CLAIM_NUMBER = claimNum,
                            PROJECT_NUMBER = projectNumber,
                            RETROFIT_NUMBER = retrofitNumber
                        },
                        P_OE_LINE_TBL = new POeLineTbl
                        {
                            OE_LINE_REC = OE_LINE_REC
                        }
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
            Dictionary<string, object> data = (Dictionary<string, object>)WebServiceRequest.JsonDeSerialize(jsonResp);
            Dictionary<string, object> outputParam = (Dictionary<string, object>)data["OutputParameters"];
            Dictionary<string, object> returnTbl = (Dictionary<string, object>)outputParam["P_RETURN_TBL"];
            Dictionary<string, object> returnTblItem = (Dictionary<string, object>)returnTbl["P_RETURN_TBL_ITEM"];
            if (returnTblItem["HASERROR"].ToString() == "1")
            {
                WorkspaceAddIn.InfoLog(returnTblItem["DESCRIPTION"].ToString());
                return;
            }
            else
            {
                _rnConnectService.UpdatePartsRecord(_partsInfo, returnTblItem["SALES_ORDER_NO"].ToString());
                //WorkspaceAddIn.InfoLog("Order sent successfully");
            }
        }
    }
}
