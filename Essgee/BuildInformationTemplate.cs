using System;

namespace Essgee
{
	static partial class BuildInformation
	{
		static BuildInformation()
		{
			data.Add("BuildDate", new DateTime(0));
			data.Add("BuildTimeZone", null);
			data.Add("GitBranch", string.Empty);
			data.Add("GitPendingChanges", false);
			data.Add("LatestCommitHash", string.Empty);
			data.Add("BuildMachineName", string.Empty);
			data.Add("BuildMachineOSPlatform", string.Empty);
			data.Add("BuildMachineOSVersion", string.Empty);
			data.Add("BuildMachineProcessorArchitecture", string.Empty);
		}
	}
}
