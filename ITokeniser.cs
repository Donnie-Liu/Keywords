using System.Collections.Generic;

namespace Keywords
{
    /// <summary>
    /// �ִ����ӿ�
    /// </summary>
    public interface ITokeniser
    {
        IList<string> Partition(string input);
    }
}