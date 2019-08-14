#include "pch.h"
#include <iostream>
#include <sstream>
#include <string>
#include <vector>
#include <memory>
#include "mscoree.h"

namespace {
	static WNDPROC s_srcProc = NULL;
	static void* s_pStartInfo = NULL;
	static bool s_bExecutiong = FALSE;
	CRITICAL_SECTION s_csConnection;

	const int ERR_UNPREDICATABLE_CLR_VERSION = 1;
	const int WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE = 0x8100;

	void SplitArguments(const std::wstring& s, wchar_t delim, std::vector<std::wstring>& elems) {
		std::wstringstream ss(s);
		std::wstring item;
		while (std::getline(ss, item, delim)) {
			elems.push_back(item);
		}
	}

	//マネージドメソッド呼び出し。
	BOOL ExecuteInDefaultAppDomain(
		LPCWSTR szCoreclrPath,
		LPCWSTR szAssembly,
		LPCWSTR szTypeFullName,
		LPCWSTR szMethod,
		LPCWSTR szArgs,
		int& errNo)
	{
		HMODULE coreClr = LoadLibraryExW(szCoreclrPath, nullptr, 0);
		if (coreClr == nullptr) {
			return FALSE;
		}

		FnGetCLRRuntimeHost pfnGetCLRRuntimeHost = (FnGetCLRRuntimeHost)::GetProcAddress(coreClr, "GetCLRRuntimeHost");
		if (pfnGetCLRRuntimeHost == nullptr) {
			return FALSE;
		}

		ICLRRuntimeHost4* runtimeHost;
		HRESULT hr = pfnGetCLRRuntimeHost(IID_ICLRRuntimeHost4, (IUnknown * *)& runtimeHost);
		if (FAILED(hr)) {
			return FALSE;
		}

		hr = runtimeHost->Start();
		if (FAILED(hr)) {
			return FALSE;
		}

		DWORD dwResult;
		hr = runtimeHost->ExecuteInDefaultAppDomain(
			szAssembly,
			szTypeFullName,
			szMethod,
			szArgs,
			&dwResult);
		return !FAILED(hr);
	}


	/**
		@brief	初期化
		@param pStartInfo 開始情報
		@return 結果
	*/
	void InitializeFriendlyCore(void* pStartInfo)
	{
		//引数パース
		std::vector<std::wstring> vec;
		SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
		if (vec.size() != 7) {
			return;
		}

		//処理実行
		int errNo = 0;
		if (!ExecuteInDefaultAppDomain(vec[2].c_str(), vec[3].c_str(), vec[4].c_str(), vec[5].c_str(), vec[6].c_str(), errNo)) {
			//失敗通知
			wchar_t* p = NULL;
			HWND hReturn = (HWND)std::wcstoull(vec[1].c_str(), &p, 10);
			::SendMessage(hReturn, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, 0, errNo);
		}
	}

	//SetWindowLongの最適呼び出し
	LONG_PTR SetWindowLongPtrEx(HWND hwnd, int index, LONG_PTR ptr)
	{
		return (::IsWindowUnicode(hwnd)) ? SetWindowLongPtrW(hwnd, index, ptr) : SetWindowLongPtrA(hwnd, index, ptr);
	}

	//CallWindowProcの最適呼び出し
	LRESULT CallWindowProcEx(WNDPROC lpPrevWndFunc, HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam)
	{
		return (::IsWindowUnicode(hWnd)) ? CallWindowProcW(lpPrevWndFunc, hWnd, Msg, wParam, lParam) : CallWindowProcA(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
	}

	LRESULT CALLBACK ExecuteConnectionProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
	{
		if (s_bExecutiong && msg == WM_NULL && wParam == 0 && lParam == 0) {
			InitializeFriendlyCore(s_pStartInfo);
			s_bExecutiong = FALSE;
		}
		return CallWindowProcEx(s_srcProc, hWnd, msg, wParam, lParam);
	}
}

BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)
{
	switch (ul_reason_for_call)
	{
	case DLL_PROCESS_ATTACH:
		::InitializeCriticalSection(&s_csConnection);
	case DLL_THREAD_ATTACH:
	case DLL_THREAD_DETACH:
	case DLL_PROCESS_DETACH:
		break;
	}
	return TRUE;
}

/**
	@brief	初期化
	@param pStartInfo 開始情報
	@return 結果
*/
DWORD __stdcall InitializeFriendly(void* pStartInfo)
{
	struct Scop {
		Scop() { ::EnterCriticalSection(&s_csConnection); }
		~Scop() { ::LeaveCriticalSection(&s_csConnection); }
	}scope;

	//引数パース
	std::vector<std::wstring> vec;
	SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
	if (vec.size() != 7) {
		return 0;
	}

	//対象ウィンドウのスレッドに処理を実行させる
	wchar_t* p = NULL;
	HWND hTargetWindow = (HWND)std::wcstoull(vec[0].c_str(), &p, 10);
	s_pStartInfo = pStartInfo;
	s_bExecutiong = TRUE;
	s_srcProc = (WNDPROC)SetWindowLongPtrEx(hTargetWindow, GWLP_WNDPROC, (LONG_PTR)ExecuteConnectionProc);
	while (s_bExecutiong && IsWindow(hTargetWindow)) {
		::SendMessage(hTargetWindow, WM_NULL, 0, 0);
		Sleep(1);
	}

	SetWindowLongPtrEx(hTargetWindow, GWLP_WNDPROC, (LONG_PTR)s_srcProc);
	if (s_bExecutiong) {
		wchar_t* p = NULL;
		HWND hReturn = (HWND)std::wcstoull(vec[1].c_str(), &p, 10);
		::SendMessage(hReturn, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, 0, 0);
	}
	return 0;
}