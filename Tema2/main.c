#include<Windows.h>
#include<stdio.h>

//Să se citească toate subcheile unei chei (la alegere) din Registry și să se afișeze la ieșirea standard. 

#define MAX_SIZE 1000

void PrintSubKeys(HKEY *hKey)
{
	char subKeyName[MAX_SIZE];
	DWORD subKeyNameSize = MAX_SIZE;
	DWORD index = 0;
	while (RegEnumKeyExA(*hKey, index, subKeyName, &subKeyNameSize, NULL, NULL, NULL, NULL) == ERROR_SUCCESS)
	{
		printf("%s\n", subKeyName);
		subKeyNameSize = MAX_SIZE;
		index++;
	}
}

int main()
{
	HANDLE hStdin = GetStdHandle(STD_INPUT_HANDLE);
	char chInput[MAX_SIZE];
	DWORD iBytesRead = 0;
	HKEY hKey = 0;
	if (ReadConsoleA(hStdin, chInput, sizeof(chInput) - 1, &iBytesRead, NULL)) 
	{
		if (iBytesRead >= 2 && chInput[iBytesRead - 1] == '\n' && chInput[iBytesRead - 2] == '\r')
		{
			chInput[iBytesRead - 2] = '\0';
		}
		else
		{
			chInput[iBytesRead] = '\0';
		}
	}
	if (RegOpenKeyExA(HKEY_CURRENT_USER, chInput, 0, KEY_ALL_ACCESS, &hKey) == ERROR_SUCCESS)
	{
		PrintSubKeys(&hKey);
		RegCloseKey(hKey);
	}
	else
	{
		MessageBoxA(NULL, "Failed to open registry key :((", "Error", MB_OK | MB_ICONERROR);
		MessageBoxA(NULL, chInput, "What was received", MB_OK | MB_ICONERROR);
	}
}