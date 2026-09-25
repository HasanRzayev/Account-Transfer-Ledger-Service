using System.Data;
using Dapper;

namespace AccountTransferLedger.Infrastructure.Persistence;

public class GuidTypeHandler : SqlMapper.TypeHandler<Guid>
{
    public override void SetValue(IDbDataParameter parameter, Guid value)
    {
        parameter.Value = value.ToString();
    }

    public override Guid Parse(object value)
    {
        return value switch
        {
            Guid g => g,
            string s when Guid.TryParse(s, out var guid) => guid,
            byte[] bytes when bytes.Length == 16 => new Guid(bytes),
            _ => Guid.Parse(value.ToString()!)
        };
    }
}
