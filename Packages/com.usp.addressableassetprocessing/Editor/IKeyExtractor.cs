using System.Collections.Generic;

public interface IKeyExtractor<I, O>
{
    void Extract(I value, O result);
}
