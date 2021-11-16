#include "stdafx.h"
#include <metahost.h>
#include <sstream>
#include <string>
#include <vector>
#include <memory>
#pragma comment(lib, "mscoree.lib")
#import "mscorlib.tlb" raw_interfaces_only \
	high_property_prefixes("_get", "_put", "_putref") \
	rename("ReportEvent", "InteropServices_ReportEvent")
using namespace mscorlib;

namespace {
	
	const int ERR_UNPREDICATABLE_CLR_VERSION = 1;
	const int WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE = 0x8100;

	void SplitArguments(const std::wstring &s, wchar_t delim, std::vector<std::wstring> &elems) {
		std::wstringstream ss(s);
		std::wstring item;
		while (std::getline(ss, item, delim)) {
			elems.push_back(item);
		}
	}

	//CLRのインターフェイスの初期化と終了処理。
	struct CLRInterfaces
	{
		ICLRMetaHost *pMetaHost;
		ICLRRuntimeInfo *pRuntimeInfo;
		ICLRRuntimeHost *pClrRuntimeHost;
		CLRInterfaces() : pMetaHost(), pRuntimeInfo(), pClrRuntimeHost() {}

		BOOL GetRuntime(PCWSTR szVersion, int& errNo, HANDLE& hProc) {
			hProc = NULL;

			//CLRバージョン指定なしの場合
			std::vector<std::wstring> elems;
			SplitArguments(szVersion, '@', elems);
			if (elems.size() == 2) {
				//現在ロードされているランタイムがあれば、それを使う
				IEnumUnknown * pRtEnum = NULL;
			
				hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, GetCurrentProcessId());
				if (!hProc) {
					::MessageBox(nullptr, L"1", L"", MB_OK);
					return FALSE;
				}
				
				while(true) {
					HRESULT hr = pMetaHost->EnumerateLoadedRuntimes(hProc, &pRtEnum);
					if (!FAILED(hr)) {//起動の瞬間とかぶったら失敗することがある。
						break;
					}
					::Sleep(10);
				}

				//ランタイムを取得
				ULONG fetched = 0;
				HRESULT hr = pRtEnum->Next(1, (IUnknown**)&pRuntimeInfo, &fetched);
				if (hr == S_OK && 0 < fetched) {
					//成功の場合はもう一度取得。
					fetched = 0;
					ICLRRuntimeInfo *pRuntimeInfo2 = NULL;
					hr = pRtEnum->Next(1, (IUnknown**)&pRuntimeInfo2, &fetched);
					pRtEnum->Release();

					//取得できた場合は推測不能なのでエラーとする。
					if (fetched == 0) {
						return TRUE;
					} else if (hr == S_OK) {
						errNo = ERR_UNPREDICATABLE_CLR_VERSION;
						pRuntimeInfo2->Release();
						::MessageBox(nullptr, L"2", L"", MB_OK);
						return FALSE;
					}
				} else {
					pRtEnum->Release();
				}

				//ない場合は操作側プロセスのランタイムに合わせる。
				szVersion = elems[1].c_str();
			}

			//指定のランタイム取得。
			HRESULT hr = pMetaHost->GetRuntime(szVersion, IID_PPV_ARGS(&pRuntimeInfo));
			return !FAILED(hr);
		}

		BOOL Init(PCWSTR szVersion, int& errNo) {
			//CLRCreateInstanceは4.0がインストールされていなければ存在しない
			auto hMod = LoadLibrary(L"MSCorEE.dll");
			auto clrCreateInstance = (CLRCreateInstanceFnPtr)GetProcAddress(hMod, "CLRCreateInstance");

			HRESULT hr;
			if (clrCreateInstance) {
				hr = clrCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&pMetaHost));
				if (FAILED(hr)) {
					clrCreateInstance = nullptr;
				}
			}

			if (!clrCreateInstance) {
				//旧式のランタイム取得方式
				std::vector<std::wstring> elems;
				SplitArguments(szVersion, '@', elems);
				if (elems.size() == 2) {
					szVersion = L"v2.0.50727";
				}
				typedef HRESULT(__stdcall *CorBindToRuntimeExType)(LPCWSTR pwszVersion, LPCWSTR pwszBuildFlavor, DWORD startupFlags, REFCLSID rclsid, REFIID riid, LPVOID FAR *ppv);
				auto corBindToRuntimeEx = (CorBindToRuntimeExType)GetProcAddress(hMod, "CorBindToRuntimeEx");
				if (!corBindToRuntimeEx) {
					::MessageBox(nullptr, L"3", L"", MB_OK);
					return FALSE;
				}
				hr = corBindToRuntimeEx(szVersion, nullptr, 0, CLSID_CLRRuntimeHost, IID_PPV_ARGS(&pClrRuntimeHost));
				if (FAILED(hr)) {
					::MessageBox(nullptr, L"4", L"", MB_OK);
					return FALSE;
				}
				hr = pClrRuntimeHost->Start();
				if (FAILED(hr)) {
					::MessageBox(nullptr, L"5", L"", MB_OK);
					return FALSE;
				}
				return TRUE;
			}

			HANDLE hProc = NULL;
			BOOL bRuntimeGet = GetRuntime(szVersion, errNo, hProc);
			if (hProc) {
				CloseHandle(hProc);
			}
			if (!bRuntimeGet) {
				::MessageBox(nullptr, L"6", L"", MB_OK);
				return FALSE;
			}

			BOOL fLoadable;
			hr = pRuntimeInfo->IsLoadable(&fLoadable);
			if (FAILED(hr) || !fLoadable) {
				::MessageBox(nullptr, L"7", L"", MB_OK);
				return FALSE;
			}

			hr = pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&pClrRuntimeHost));
			if (FAILED(hr)) {
				::MessageBox(nullptr, L"8", L"", MB_OK);
				return FALSE;
			}

			hr = pClrRuntimeHost->Start();
			if (FAILED(hr)) {
				::MessageBox(nullptr, L"9", L"", MB_OK);
				return FALSE;
			}
			return TRUE;
		}

		~CLRInterfaces() {
			if (pMetaHost) {
				pMetaHost->Release();
				pMetaHost = NULL;
			}
			if (pRuntimeInfo) {
				pRuntimeInfo->Release();
				pRuntimeInfo = NULL;
			}
			if (pClrRuntimeHost) {
				pClrRuntimeHost->Release();
				pClrRuntimeHost = NULL;
			}
		}
	};

	//マネージドメソッド呼び出し。
	BOOL ExecuteInDefaultAppDomain(
		LPCWSTR szVersion,
		LPCWSTR szAssembly,
		LPCWSTR szTypeFullName,
		LPCWSTR szMethod,
		LPCWSTR szArgs,
		int& errNo)
	{
		CLRInterfaces interfaces;
		if (!interfaces.Init(szVersion, errNo)) {
			::MessageBox(nullptr, L"10", L"", MB_OK);
			return FALSE;
		}
		
		DWORD dwResult = 0;
		HRESULT hr = interfaces.pClrRuntimeHost->ExecuteInDefaultAppDomain(
			szAssembly,
			szTypeFullName,
			szMethod,
			szArgs,
			&dwResult);
		return !FAILED(hr);
	}

	//指定のドメインでインスタンスを生成して、そのメソッドを呼び出し
	//メソッドの型は int method(string arg);
	BOOL ExecuteInstanceMethod(
		_AppDomainPtr pDomain,
		LPCWSTR szAssemblyPath,
		LPCWSTR szClass,
		LPCWSTR szMethod,
		LPCWSTR szArg,
		variant_t& ret)
	{
		//破棄用
		struct Data {
			_ObjectHandle* phObj;
			VARIANT varObj;
			VARIANT* pVarObj;
			_ObjectPtr pObj;
			SAFEARRAY* pArgs;
			_TypePtr pType;

			Data() : phObj(), pVarObj(), pObj(), pArgs(), pType() {}

			~Data() {
				if (pArgs)::SafeArrayDestroy(pArgs);
				if (pType)pType->Release();
				if (pObj)pObj->Release();
				if (pVarObj)VariantClear(pVarObj);
				if (phObj)phObj->Release();
			}
		}data;

		//インスタンス生成
		auto hr = pDomain->CreateInstanceFrom(_bstr_t(szAssemblyPath), _bstr_t(szClass), &data.phObj);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"11", L"", MB_OK);
			return FALSE;
		}

		hr = data.phObj->Unwrap(&data.varObj);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"12", L"", MB_OK);
			return FALSE;
		}
		data.pVarObj = &data.varObj;

		hr = data.varObj.pdispVal->QueryInterface(IID_PPV_ARGS(&data.pObj));
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"13", L"", MB_OK);
			return FALSE;
		}

		//引数作成
		data.pArgs = SafeArrayCreateVector(VT_VARIANT, 0, 1);
		VARIANT* pVarArg;
		hr = SafeArrayAccessData(data.pArgs, reinterpret_cast<void**>(&pVarArg));
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"14", L"", MB_OK);
			return FALSE;
		}

		V_VT(pVarArg) = VT_BSTR;
		V_BSTR(pVarArg) = _bstr_t(szArg);

		hr = SafeArrayUnaccessData(data.pArgs);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"15", L"", MB_OK);
			return FALSE;
		}

		//タイプ取得
		hr = data.pObj->GetType(&data.pType);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"16", L"", MB_OK);
			return FALSE;
		}

		//リフレクションでメソッド実行
		hr = data.pType->InvokeMember_3(_bstr_t(szMethod), static_cast<BindingFlags>(
			BindingFlags_InvokeMethod | BindingFlags_Instance | BindingFlags_Public),
			nullptr, data.varObj, data.pArgs, &ret);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"17", L"", MB_OK);
			return FALSE;
		}
		return TRUE;
	}

	//アプリケーションドメインの列挙
	//指定があれば合致するドメインで開始処理を実行
	BOOL EnumDomainsAndExecute(
		LPCWSTR szVersion,
		LPCWSTR szStepAssembly,
		LPCWSTR szGetIDClass,
		LPCWSTR szGetIDMethod,
		std::vector<int>& ids,
		int executeDomainId,
		LPCWSTR szStartClass,
		LPCWSTR szStartMethod,
		LPCWSTR szStartArgs)
	{
		CLRInterfaces interfaces;
		int errNo = 0;
		if (!interfaces.Init(szVersion, errNo) || !interfaces.pRuntimeInfo) {
			::MessageBox(nullptr, L"18", L"", MB_OK);
			return FALSE;
		}

		//破棄用
		struct Data {
			ICorRuntimeHost* pCorRuntimeHost;
			HDOMAINENUM hEnum;
			IUnknownPtr pDomainSrc;
			_AppDomainPtr pDomain;
			Data() : pCorRuntimeHost(), hEnum(), pDomainSrc(), pDomain() {}
			~Data() {
				if (hEnum) pCorRuntimeHost->CloseEnum(hEnum);
				if (pCorRuntimeHost) pCorRuntimeHost->Release();
			}
			void ReleaseDomain() {
				pDomainSrc = nullptr;
				pDomain = nullptr;
			}
		}data;

		//ドメイン列挙
		auto hr = interfaces.pRuntimeInfo->GetInterface(CLSID_CorRuntimeHost, IID_PPV_ARGS(&data.pCorRuntimeHost));
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"19", L"", MB_OK);
			return FALSE;
		}

		hr = data.pCorRuntimeHost->EnumDomains(&data.hEnum);
		if (FAILED(hr)) {
			::MessageBox(nullptr, L"20", L"", MB_OK);
			return FALSE;
		}

		while (data.pCorRuntimeHost->NextDomain(data.hEnum, &data.pDomainSrc) == S_OK) {
			hr = data.pDomainSrc->QueryInterface(IID_PPV_ARGS(&data.pDomain));
			if (FAILED(hr)) {
				::MessageBox(nullptr, L"21", L"", MB_OK);
				return FALSE;
			}

			//ドメインのID取得
			variant_t ret;
			if (!ExecuteInstanceMethod(
				data.pDomain,
				szStepAssembly,
				szGetIDClass,
				szGetIDMethod,
				L"",
				ret))
			{
				::MessageBox(nullptr, L"22", L"", MB_OK);
				return FALSE;
			}
			ids.push_back(ret.intVal);

			//指定があれば開始処理実行
			if (szStartClass && ret.intVal == executeDomainId) {
				if (!ExecuteInstanceMethod(
					data.pDomain,
					szStepAssembly,
					szStartClass,
					szStartMethod,
					szStartArgs,
					ret))
				{
					::MessageBox(nullptr, L"23", L"", MB_OK);
					return FALSE;
				}
			}
			data.ReleaseDomain();
		}
		return TRUE;
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

	//接続関数を実行
	static WNDPROC s_srcProc = NULL;
	static void* s_pStartInfo = NULL;
	static bool s_bExecutiong = FALSE;
	LRESULT CALLBACK ExecuteConnectionProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam)
	{
		if (s_bExecutiong && msg == WM_NULL && wParam == 0 && lParam == 0) {
			InitializeFriendlyCore(s_pStartInfo);
			s_bExecutiong = FALSE;
		}
		return CallWindowProcEx(s_srcProc, hWnd, msg, wParam, lParam);
	}
}

CRITICAL_SECTION s_csConnection;

/**
	@brief	初期化
	@param pStartInfo 開始情報
	@return 結果
*/
DWORD __stdcall InitializeFriendly(void* pStartInfo)
{
	struct Scop {
		Scop(){ ::EnterCriticalSection(&s_csConnection); }
		~Scop(){ ::LeaveCriticalSection(&s_csConnection); }
	}scope;

	::MessageBox(nullptr, (LPCWSTR)pStartInfo, L"start", MB_OK);

	//引数パース
	std::vector<std::wstring> vec;
	SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
	if (vec.size() != 7) {
		::MessageBox(nullptr, L"24", L"", MB_OK);
		return 0;
	}

	//対象ウィンドウのスレッドに処理を実行させる
	wchar_t* p = NULL;
	HWND hTargetWindow = (HWND)std::wcstoull(vec[0].c_str(), &p, 10);
	s_pStartInfo = pStartInfo;
	s_bExecutiong = TRUE;
	s_srcProc = (WNDPROC)SetWindowLongPtrEx(hTargetWindow, GWLP_WNDPROC, (LONG_PTR)ExecuteConnectionProc);
	while(s_bExecutiong && IsWindow(hTargetWindow)) {
		::SendMessage(hTargetWindow, WM_NULL, 0, 0);
		Sleep(1);
	}
	SetWindowLongPtrEx(hTargetWindow, GWLP_WNDPROC, (LONG_PTR)s_srcProc);
	if (s_bExecutiong) {
		wchar_t* p = NULL;
		HWND hReturn = (HWND)std::wcstoull(vec[1].c_str(), &p, 10);
		::SendMessage(hReturn, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, 0, 0);
		::MessageBox(nullptr, L"25", L"", MB_OK);
	}
	return 0;
}

//ドメイン列挙
int __stdcall EnumDomains(
	LPCWSTR szVersion,
	LPCWSTR szStepAssembly,
	LPCWSTR szGetIDClass,
	LPCWSTR szGetIDMethod,
	int* ids,
	int size)
{
	std::vector<int> vecIds;
	if (!EnumDomainsAndExecute(szVersion, szStepAssembly, szGetIDClass, szGetIDMethod, vecIds, -1, nullptr, nullptr, nullptr)) {
		return -1;
	}
	for (int i = 0; i < (int)vecIds.size() && i < size; i++) {
		ids[i] = vecIds[i];
	}
	return (int)vecIds.size();
}

//指定のドメインをFriendlyでの操作用に初期化
BOOL __stdcall InitializeAppDomain(
	LPCWSTR szVersion,
	LPCWSTR szStepAssembly,
	LPCWSTR szGetIDClass,
	LPCWSTR szGetIDMethod,
	LPCWSTR szStartClass,
	LPCWSTR szStartMethod,
	int id,
	LPCWSTR pStartInfo)
{
	std::vector<std::wstring> vec;
	SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
	if (vec.size() != 7) {
		::MessageBox(nullptr, L"25", L"", MB_OK);
		return FALSE;
	}
	std::vector<int> vecIds;
	if (!EnumDomainsAndExecute(szVersion, szStepAssembly, szGetIDClass, szGetIDMethod, vecIds, id, szStartClass, szStartMethod, vec[6].c_str())) {
		::MessageBox(nullptr, L"26", L"", MB_OK);
		return FALSE;
	}
	return TRUE;
}

DWORD __stdcall HasGetCLRRuntimeHost(void* pStartInfo)
{
	auto path = (LPCWSTR)pStartInfo;
	auto hmodule = ::LoadLibrary(path);

	BOOL hasGetCLRRuntimeHost = ::GetProcAddress(hmodule, "GetCLRRuntimeHost") != nullptr;

	::FreeLibrary(hmodule);

	return hasGetCLRRuntimeHost;
}