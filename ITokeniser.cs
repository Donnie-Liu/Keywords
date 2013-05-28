using System.Collections.Generic;

namespace Keywords
{
    /// <summary>
    /// ·Ö´ÊÆ÷½Ó¿Ú
    /// </summary>
    public interface ITokeniser
    {
        IList<string> Partition(string input);
    }
}