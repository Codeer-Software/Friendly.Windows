#include "stdafx.h"
#include <metahost.h>
#include <sstream>
#include <string>
#include <vector>

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

	//CLR�̃C���^�[�t�F�C�X�̏������ƏI�������B
	struct CLRInterfaces
	{
		ICLRMetaHost *pMetaHost;
		ICLRRuntimeInfo *pRuntimeInfo;
		ICLRRuntimeHost *pClrRuntimeHost;
		CLRInterfaces() : pMetaHost(), pRuntimeInfo(), pClrRuntimeHost() {}

		BOOL GetRuntime(PCWSTR szVersion, int& errNo, HANDLE& hProc) {
			hProc = NULL;

			//CLR�o�[�W�����w��Ȃ��̏ꍇ
			std::vector<std::wstring> elems;
			SplitArguments(szVersion, '@', elems);
			if (elems.size() == 2) {
				//���݃��[�h����Ă��郉���^�C��������΁A������g��
				IEnumUnknown * pRtEnum = NULL;
			
				hProc = OpenProcess(PROCESS_QUERY_INFORMATION, FALSE, GetCurrentProcessId());
				if (!hProc) {
					return FALSE;
				}
				
				while(true) {
					HRESULT hr = pMetaHost->EnumerateLoadedRuntimes(hProc, &pRtEnum);
					if (!FAILED(hr)) {//�N���̏u�ԂƂ��Ԃ����玸�s���邱�Ƃ�����B
						break;
					}
					::Sleep(10);
				}

				//�����^�C�����擾
				ULONG fetched = 0;
				HRESULT hr = pRtEnum->Next(1, (IUnknown**)&pRuntimeInfo, &fetched);
				if (hr == S_OK && 0 < fetched) {
					//�����̏ꍇ�͂�����x�擾�B
					fetched = 0;
					ICLRRuntimeInfo *pRuntimeInfo2 = NULL;
					hr = pRtEnum->Next(1, (IUnknown**)&pRuntimeInfo2, &fetched);
					pRtEnum->Release();

					//�擾�ł����ꍇ�͐����s�\�Ȃ̂ŃG���[�Ƃ���B
					if (fetched == 0) {
						return TRUE;
					} else if (hr == S_OK) {
						errNo = ERR_UNPREDICATABLE_CLR_VERSION;
						pRuntimeInfo2->Release();
						return FALSE;
					}
				} else {
					pRtEnum->Release();
				}

				//�Ȃ��ꍇ�͑��쑤�v���Z�X�̃����^�C���ɍ��킹��B
				szVersion = elems[1].c_str();
			}

			//�w��̃����^�C���擾�B
			HRESULT hr = pMetaHost->GetRuntime(szVersion, IID_PPV_ARGS(&pRuntimeInfo));
			return !FAILED(hr);
		}


		BOOL Init(PCWSTR szVersion, int& errNo) {
			HRESULT hr = CLRCreateInstance(CLSID_CLRMetaHost, IID_PPV_ARGS(&pMetaHost));
			if (FAILED(hr)) {
				return FALSE;
			}

			HANDLE hProc = NULL;
			BOOL bRuntimeGet = GetRuntime(szVersion, errNo, hProc);
			if (hProc) {
				CloseHandle(hProc);
			}
			if (!bRuntimeGet) {
				return FALSE;
			}

			BOOL fLoadable;
			hr = pRuntimeInfo->IsLoadable(&fLoadable);
			if (FAILED(hr) || !fLoadable) {
				return FALSE;
			}

			hr = pRuntimeInfo->GetInterface(CLSID_CLRRuntimeHost, IID_PPV_ARGS(&pClrRuntimeHost));
			if (FAILED(hr)) {
				return FALSE;
			}

			hr = pClrRuntimeHost->Start();
			if (FAILED(hr)) {
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

	//�}�l�[�W�h���\�b�h�Ăяo���B
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

	/**
		@brief	������
		@param pStartInfo �J�n���
		@return ����
	*/
	void InitializeFriendlyCore(void* pStartInfo)
	{
		//�����p�[�X
		std::vector<std::wstring> vec;
		SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
		if (vec.size() != 7) {
			return;
		}

		//�������s
		int errNo = 0;
		if (!ExecuteInDefaultAppDomain(vec[2].c_str(), vec[3].c_str(), vec[4].c_str(), vec[5].c_str(), vec[6].c_str(), errNo)) {
			//���s�ʒm
			wchar_t* p = NULL;
			HWND hReturn = (HWND)std::wcstoul(vec[1].c_str(), &p, 10);
			::SendMessage(hReturn, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, 0, errNo);
		}
	}

	//SetWindowLong�̍œK�Ăяo��
	LONG_PTR SetWindowLongPtrEx(HWND hwnd, int index, LONG_PTR ptr)
	{
		return (::IsWindowUnicode(hwnd)) ? SetWindowLongPtrW(hwnd, index, ptr) : SetWindowLongPtrA(hwnd, index, ptr);
	}

	//CallWindowProc�̍œK�Ăяo��
	LRESULT CallWindowProcEx(WNDPROC lpPrevWndFunc, HWND hWnd, UINT Msg, WPARAM wParam, LPARAM lParam)
	{
		return (::IsWindowUnicode(hWnd)) ? CallWindowProcW(lpPrevWndFunc, hWnd, Msg, wParam, lParam) : CallWindowProcA(lpPrevWndFunc, hWnd, Msg, wParam, lParam);
	}

	//�ڑ��֐������s
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
	@brief	������
	@param pStartInfo �J�n���
	@return ����
*/
DWORD __stdcall InitializeFriendly(void* pStartInfo)
{
	struct Scop {
		Scop(){ ::EnterCriticalSection(&s_csConnection); }
		~Scop(){ ::LeaveCriticalSection(&s_csConnection); }
	}scope;

	//�����p�[�X
	std::vector<std::wstring> vec;
	SplitArguments((LPCWSTR)pStartInfo, '\t', vec);
	if (vec.size() != 7) {
		return 0;
	}

	//�ΏۃE�B���h�E�̃X���b�h�ɏ��������s������
	wchar_t* p = NULL;
	HWND hTargetWindow = (HWND)std::wcstoul(vec[0].c_str(), &p, 10);
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
		HWND hReturn = (HWND)std::wcstoul(vec[1].c_str(), &p, 10);
		::SendMessage(hReturn, WM_NOTIFY_SYSTEM_CONTROL_WINDOW_HANDLE, 0, 0);
	}
	return 0;
}
