using System;
using Codeer.Friendly.Windows.Properties;

namespace Codeer.Friendly.Windows.Inside
{
    /// <summary>
    /// ローカライズ済みリソース。
    /// </summary>
    [Serializable]
    public class ResourcesLocal
    {
        static internal ResourcesLocal Instance;

        /// <summary>
        /// ErrorAppCommunication
        /// </summary>
        public string ErrorAppCommunication { get; set; }

        /// <summary>
        /// ErrorAppConnection
        /// </summary>
        public string ErrorAppConnection { get; set; }

        /// <summary>
        /// ErrorArgumentInvokeFormat
        /// </summary>
        public string ErrorArgumentInvokeFormat { get; set; }

        /// <summary>
        /// ErrorArgumentInvokeFormatForObjectArray
        /// </summary>
        public string ErrorArgumentInvokeFormatForObjectArray { get; set; }

        /// <summary>
        /// ErrorBinaryInstall
        /// </summary>
        public string ErrorBinaryInstall { get; set; }

        /// <summary>
        /// ErrorDllLoad
        /// </summary>
        public string ErrorDllLoad { get; set; }

        /// <summary>
        /// ErrorExecuteThreadWindowHandle
        /// </summary>
        public string ErrorExecuteThreadWindowHandle { get; set; }

        /// <summary>
        /// ErrorFriendlySystem
        /// </summary>
        public string ErrorFriendlySystem { get; set; }

        /// <summary>
        /// ErrorInvalidThreadCall
        /// </summary>
        public string ErrorInvalidThreadCall { get; set; }

        /// <summary>
        /// ErrorManyFoundConstractorFormat
        /// </summary>
        public string ErrorManyFoundConstractorFormat { get; set; }

        /// <summary>
        /// ErrorManyFoundInvokeFormat
        /// </summary>
        public string ErrorManyFoundInvokeFormat { get; set; }

        /// <summary>
        /// ErrorNotFoundConstractorFormat
        /// </summary>
        public string ErrorNotFoundConstractorFormat { get; set; }

        /// <summary>
        /// ErrorNotFoundConstractorFormatForObjectArray
        /// </summary>
        public string ErrorNotFoundConstractorFormatForObjectArray { get; set; }

        /// <summary>
        /// ErrorNotFoundInvokeFormat
        /// </summary>
        public string ErrorNotFoundInvokeFormat { get; set; }

        /// <summary>
        /// ErrorOperationTypeArgInfoFormat
        /// </summary>
        public string ErrorOperationTypeArgInfoFormat { get; set; }

        /// <summary>
        /// ErrorOperationTypeArgInfoForObjectArrayFormat
        /// </summary>
        public string ErrorOperationTypeArgInfoForObjectArrayFormat { get; set; }

        /// <summary>
        /// ErrorProcessAcess
        /// </summary>
        public string ErrorProcessAcess { get; set; }

        /// <summary>
        /// ErrorProcessOperation
        /// </summary>
        public string ErrorProcessOperation { get; set; }

        /// <summary>
        /// ErrorTargetCpuDifference
        /// </summary>
        public string ErrorTargetCpuDifference { get; set; }

        /// <summary>
        /// ErrorUnpredicatableClrVersion
        /// </summary>
        public string ErrorUnpredicatableClrVersion { get; set; }

        /// <summary>
        /// HasNotEnumerable
        /// </summary>
        public string HasNotEnumerable { get; set; }

        /// <summary>
        /// NullObjectOperation
        /// </summary>
        public string NullObjectOperation { get; set; }

        /// <summary>
        /// OutOfCommunicationNo
        /// </summary>
        public string OutOfCommunicationNo { get; set; }

        /// <summary>
        /// OutOfMemory
        /// </summary>
        public string OutOfMemory { get; set; }

        /// <summary>
        /// UnknownTypeInfoFormat
        /// </summary>
        public string UnknownTypeInfoFormat { get; set; }

        /// <summary>
        /// ErrorAttachOtherDomainsNeedNet4
        /// </summary>
        public string ErrorAttachOtherDomainsNeedNet4 { get; set; }

        /// <summary>
        /// 特別。
        /// 初期化前に呼ばれる。
        /// また、必ず操作側プロセスから使われるのでResourcesを直に使う。
        /// </summary>
        static internal string ObsoleteClrOrder { get { return Resources.ObsoleteClrOrder; } }

        /// <summary>
        /// 初期化。
        /// </summary>
        internal static void Initialize()
        {
            Instance = new ResourcesLocal();
            Instance.InitializeCore();
        }

        /// <summary>
        /// 対象に文字列インストール
        /// </summary>
        /// <param name="app">アプリケーション操作クラス。</param>
        internal static void Install(WindowsAppFriend app)
        {
            app[typeof(ResourcesLocal), "Instance"](Instance);
        }

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        void InitializeCore()
        {
            ErrorAppCommunication = Resources.ErrorAppCommunication;
            ErrorAppConnection = Resources.ErrorAppConnection;
            ErrorArgumentInvokeFormat = Resources.ErrorArgumentInvokeFormat;
            ErrorArgumentInvokeFormatForObjectArray = Resources.ErrorArgumentInvokeFormatForObjectArray;
            ErrorBinaryInstall = Resources.ErrorBinaryInstall;
            ErrorDllLoad = Resources.ErrorDllLoad;
            ErrorExecuteThreadWindowHandle = Resources.ErrorExecuteThreadWindowHandle;
            ErrorFriendlySystem = Resources.ErrorFriendlySystem;
            ErrorInvalidThreadCall = Resources.ErrorInvalidThreadCall;
            ErrorManyFoundConstractorFormat = Resources.ErrorManyFoundConstractorFormat;
            ErrorManyFoundInvokeFormat = Resources.ErrorManyFoundInvokeFormat;
            ErrorNotFoundConstractorFormat = Resources.ErrorNotFoundConstractorFormat;
            ErrorNotFoundConstractorFormatForObjectArray = Resources.ErrorNotFoundConstractorFormatForObjectArray;
            ErrorNotFoundInvokeFormat = Resources.ErrorNotFoundInvokeFormat;
            ErrorOperationTypeArgInfoFormat = Resources.ErrorOperationTypeArgInfoFormat;
            ErrorOperationTypeArgInfoForObjectArrayFormat = Resources.ErrorOperationTypeArgInfoForObjectArrayFormat;
            ErrorProcessAcess = Resources.ErrorProcessAcess;
            ErrorProcessOperation = Resources.ErrorProcessOperation;
            ErrorTargetCpuDifference = Resources.ErrorTargetCpuDifference;
            ErrorUnpredicatableClrVersion = Resources.ErrorUnpredicatableClrVersion;
            HasNotEnumerable = Resources.HasNotEnumerable;
            NullObjectOperation = Resources.NullObjectOperation;
            OutOfCommunicationNo = Resources.OutOfCommunicationNo;
            OutOfMemory = Resources.OutOfMemory;
            UnknownTypeInfoFormat = Resources.UnknownTypeInfoFormat;
            ErrorAttachOtherDomainsNeedNet4 = Resources.ErrorAttachOtherDomainsNeedNet4;
        }
    }
}
