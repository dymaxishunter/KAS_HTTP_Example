using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Web;
using System.Web.Script.Serialization;
using System.Threading;

namespace KAS_HTTP_TEST {
	class Program {
		static void Main(string[] args) {
			//Import variables from KAS "Variable Text File"
			KasVariableImport vars = new KasVariableImport();
			string tagReqString = vars.importVariableFile();

			List<String> varlist = vars.tagList;
			List<String> UnusedVarlist = vars.unusedTagList;

			//Debug printout to ensure import worked correctly
			Console.WriteLine("Imported Vars: ");
			for (int i = 0; i < varlist.Count - 1; i++) {
				Console.WriteLine(string.Concat("\t", varlist[i]));
			}

			Console.WriteLine("NOT Imported Vars: ");
			for (int i = 0; i < UnusedVarlist.Count - 1; i++) {
				Console.WriteLine(string.Concat("\t", UnusedVarlist[i]));
			}

			//set up read of all variables
			KasComms KasPDMM = new KasComms("http://169.254.0.0");

			bool runLoop = true;
			//loop to request tag values - run something similar to pull all variables for specific pages
			while (runLoop) {
				//read all variables
				KasPDMM.ReadKas_Variables(tagReqString, varlist);
				//pull dictionary
				Dictionary<string, Dictionary<string, string>> tagDictionay = KasPDMM.responseDictionary;
				Console.WriteLine("Enter tag name to retrieve value. Type \"Exit\" to exit loop");
				string userRequest = Console.ReadLine();
				if (userRequest.Equals("Exit")) {
					runLoop = false;
                } else {
                    try {
						//dictionary lookup of variables
						Dictionary<string, string> attributeDictionary = tagDictionay[userRequest];
						string variableValue = attributeDictionary["value"];
						Console.WriteLine("Value: {0}", variableValue);
					}
					catch {
						Console.WriteLine("Invalid Tag Name");
                    }
                }


			}

		}
	}

	class KasVariableImport {
		public List<String> tagList = new List<string>();
		public List<String> unusedTagList = new List<string>();

		public string importVariableFile() {
			string workDir = Directory.GetCurrentDirectory();
			DirectoryInfo tmpDir = Directory.GetParent(workDir);
			//DirectoryInfo tmpDir2 = Directory.GetParent(tmpDir.FullName);
			DirectoryInfo currDir = Directory.GetParent(tmpDir.FullName);
			string path = string.Concat(currDir.FullName, "\\KASVariableList.txt");



			string fullText = System.IO.File.ReadAllText(path);
			string[] lines = fullText.Split('\n');
			string tagRequestString = "";

			for (int i = 0; i < lines.Length; i++) {
				if (lines[i].Contains(':') && lines[i].Contains(':')) {
					//if line is a valid variable
					int colonIndex = lines[i].IndexOf(':');
					string tagName = lines[i].Substring(0, colonIndex).Trim();
					string tagInfo = lines[i].Substring(colonIndex).Trim();

					//before adding to list, need to check for:
					//		1. Data type - needs to be elementary?
					//		2. Array - need to run for loop to add multiple tags for each array element - I think? 

					if (tagInfo.Contains("ARRAY")) {
						//split tagInfo string to find array length

					} else if (tagInfo.Contains("BOOL") || tagInfo.Contains("INT") || tagInfo.Contains("DINT") || tagInfo.Contains("LINT") || tagInfo.Contains("SINT")
						 || tagInfo.Contains("UINT") || tagInfo.Contains("UDINT") || tagInfo.Contains("ULINT") || tagInfo.Contains("USINT")
						 || tagInfo.Contains("WORD") || tagInfo.Contains("DWORD") || tagInfo.Contains("LWORD") || tagInfo.Contains("BYTE")
						 || tagInfo.Contains("REAL") || tagInfo.Contains("LREAL")) {

						if (tagRequestString.Equals("")) {
							//first line don't add comma before tag name
							tagRequestString = tagName;
						} else {
							tagRequestString = string.Concat(tagRequestString, ",", tagName);
						}

						tagList.Add(tagName);

					} else {
						//not an array or elementary data type - add to list of tags not being updated
						unusedTagList.Add(tagName);
					}
				}
			}

			return tagRequestString;
		}
	}

	class KasComms {
		private string IPAddr;
		public Dictionary<string, Dictionary<string, string>> responseDictionary;
		Dictionary<string, string> attributeDictionary;

		public KasComms(string IPAddress) {
			this.IPAddr = IPAddress;
		}


		public void ReadKas_Variables(String tagReqString, List<String> varList) {

			string controllerIPAddress = IPAddr;
			string httpInterfaceURL = "/kas/plcvariables";
			string format = "json"; // "text" is also supported


			string httpRequestString = controllerIPAddress + httpInterfaceURL + "?variables=" + tagReqString + "&format=" + format;


			//run this in a loop in one thread. Add new function to request variables by running these lines of code:
			//attributeDictionary = responseDictionary[TAG_NAME_HERE];
			//string variableValue = attributeDictionary["value"];

			WebClient client = new WebClient();
			try {
				String httpResponseString = client.DownloadString(httpRequestString); // Send the GET HTTP request
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				// Use JavaScriptSerializer to convert the response string into JSON specific dictionary object
				responseDictionary = serializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(httpResponseString);
				// Now responseDictionary will contain a map of variable name and its attributes (that is value and errorstatus)

				attributeDictionary = new Dictionary<string, string>();
				//To get get the value and errorstatus we can use the following way:

				/////////////////////////////////////////////////////////
				//COMMENTED LINES BELOW PRINT OUT ALL VARIABLE VALUES
				/////////////////////////////////////////////////////////

				//Console.WriteLine("\n\nCURRENT VARIABLE STATUS:\n");

				//for(int i = 0; i < (varList.Count() - 1); i++) {
				//	attributeDictionary = responseDictionary[varList[i]];
				//	string variableValue = attributeDictionary["value"];
				//	string variableErrorStatus = attributeDictionary["errorstatus"];
				//	Console.WriteLine("Variablename: {0}\n\tvalue:\t{1}", varList[i], variableValue);
				//	//Console.WriteLine("Variablename: {0}\n\tvalue:{1}\n\terrorstatus={2}", varList[i], variableValue, variableErrorStatus);
				//}
				//
				//		SET VALUES FROM HTTP REQUEST RESPONSE HERE
				//
			}
			catch (WebException someWebException) // If server returns some error code
			{
				if (null != ((HttpWebResponse)someWebException.Response)) {
					Stream reader = ((HttpWebResponse)someWebException.Response).GetResponseStream();
					byte[] message = new byte[reader.Length];
					reader.Read(message, 0, (int)reader.Length);
					string httpErrorCode = someWebException.Message;
					//string httpErrorDescription = Encoding.ASCII.GetString(message);
				}
				Console.WriteLine(someWebException.Message);
			}
			catch (Exception someException) // If some other exception happens
			{
				Console.WriteLine(someException.Message);

			}
		}

	}
}

