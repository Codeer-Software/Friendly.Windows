using System.Collections.Generic;

namespace Codeer.Friendly.DotNetExecutor
{
	/// <summary>
	/// 固有の番号管理。
	/// </summary>
	public class UniqueNoManager
	{
		int _no;
		Dictionary<int, bool> _curretExistNo = new Dictionary<int, bool>();
		
		/// <summary>
		/// 番号生成。
		/// </summary>
		/// <param name="no">番号。</param>
		/// <returns>成否。</returns>
        public bool CreateNo(out int no)
		{
			no = 0;
			_no++;
			int firstNo = _no;
			while (_no == 0 || _curretExistNo.ContainsKey(_no))
			{
				_no++;
				if (_no == firstNo)
				{
					return false;
				}
			}
			no = _no;
            _curretExistNo.Add(no, true);
			return true;
		}

		/// <summary>
		/// 番号解放。
		/// </summary>
		/// <param name="no">番号。</param>
        public void FreeNo(int no)
		{
			_curretExistNo.Remove(no);
		}
	}
}
