#include<windows.h>

/*Să se creeze un proiect în MSVC folosind limbajul C sau C++, în cadrul căruia 
se va proiecta o aplicație care citește toate datele dintr-un fișier 
al cărui path relativ este preluat de la intrarea standard 
și le va afișa la ieșirea standard. 
În cazul în care fișierul menționat nu există, se va afișa un mesaj de eroare. 
Codul va respecta notația maghiară.*/

int main()
{
	OFSTRUCT of;
	HANDLE hFile;
	HANDLE hStdin = GetStdHandle(STD_INPUT_HANDLE);
	char chBuf[2048];
	char chInput[128];
	DWORD iBytesRead = 0;
	DWORD iBytesRead2 = 0;
	if (ReadConsole(hStdin, chInput, sizeof(chInput)-1, &iBytesRead, NULL))
	{
		if (iBytesRead >= 2 && chInput[iBytesRead - 1] == '\n' && chInput[iBytesRead-2]=='\r')
		{
			chInput[iBytesRead-2] = '\0';
		}
		else
		{
			chInput[iBytesRead] = '\0';
		}
	}
	hFile = CreateFileA(chInput, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
	if (hFile == INVALID_HANDLE_VALUE)
	{
		MessageBox(NULL, "Failed to open file!", "Error", MB_OK | MB_ICONERROR);
		return 1;
	}
	else
	{	
		ReadFile(hFile, chBuf, sizeof(chBuf)-1, &iBytesRead2, NULL);
		chBuf[iBytesRead2] = '\0';
		MessageBox(NULL, chBuf, "File contents", MB_OK);
		CloseHandle(hFile);
		return 0;
	}
}