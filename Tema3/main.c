#include<Windows.h>
#include<stdio.h>
#include<stdlib.h>

//Să se proiecteze o aplicație(powershell, cmd, MSVC / C++ preferabil) care să identifice toate serviciile sistem care rulează la modul curent pe mașină.

void PrintAllServices(SC_HANDLE hSCManager)
{
	DWORD dwBytesNeeded = 0;
	DWORD dwServiceCount = 0;
	DWORD dwResumeHandle = 0;
	if (EnumServicesStatusExA(hSCManager, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_ACTIVE, NULL, 0, &dwBytesNeeded, &dwServiceCount, &dwResumeHandle, "") == 0)
	{
		DWORD err = GetLastError();
		if(err != ERROR_MORE_DATA) {
			printf("EnumServicesStatusExA failed with error code: %d\n", err);
			return;
		}
	}
	LPBYTE buf = (LPBYTE)malloc(dwBytesNeeded * sizeof(BYTE));
	if (buf == NULL)
	{
		printf("Memory allocation failed\n");
		free(buf);
		return;
	}
	if (EnumServicesStatusExA(hSCManager, SC_ENUM_PROCESS_INFO, SERVICE_WIN32, SERVICE_ACTIVE, buf, dwBytesNeeded, &dwBytesNeeded, &dwServiceCount,&dwResumeHandle, "") == 0)
	{
		printf("EnumServicesStatusExA failed with error code: %d\n", GetLastError());
		free(buf);
		return;
	}
	ENUM_SERVICE_STATUS_PROCESSA* services = (ENUM_SERVICE_STATUS_PROCESSA*)buf;
	for (DWORD i = 0; i < dwServiceCount; i++)
	{
		printf("%d\. Service Name: %s, Display Name: %s\n", i+1, services[i].lpServiceName, services[i].lpDisplayName);
	}
	free(buf);
}

int main(void)
{
	SC_HANDLE SCManager = OpenSCManager(NULL, NULL, SC_MANAGER_CONNECT | SC_MANAGER_ENUMERATE_SERVICE);
	if (!SCManager)
	{
		printf("OpenSCManager failed with error code: %d\n", GetLastError());
		return 1;
	}
	PrintAllServices(SCManager);
	CloseServiceHandle(SCManager);
	return 0;
}