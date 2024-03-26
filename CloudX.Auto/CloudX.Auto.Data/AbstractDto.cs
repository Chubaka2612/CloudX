using CloudX.Auto.Core.Utils;

namespace CloudX.Auto.AWS.Core
{
    public abstract class AbstractDto
    {
        public override string ToString()
        {
            return CommonUtils.WrapToJson(this);
        }
    }
}
