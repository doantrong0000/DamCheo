using BimSpeedUtils;

namespace BimSpeedStructureBeamDesign.Utils;

public static class BeamRebarDefine
{
    public static string GetChiTietDam()
    {
        if (Constants.Lang == LangEnum.EN)
        {
            return "Beam Detail ";
        }

        if (Constants.Lang == LangEnum.VN)
        {
            return "CHI TIẾT DẦM ";
        }

        if (Constants.Lang == LangEnum.JP)
        {
            return "鉄筋加工図 ";
        }

        if (Constants.Lang == LangEnum.SP)
        {
            return "Beam Detail";
        }




        return "Beam Detail";
    }

}