using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PilotGaea.Geometry;
using PilotGaea.TMPEngine;
using PilotGaea.Serialize;

namespace AsynchronousDoCommand
{
    public class AsynchronousDoCommand : DoCmdBaseClass
    {
        public override bool Init()
        {
            m_Cmds = new List<string>();
            m_Cmds.Add("GetAllLayerInfo");
            m_Cmds.Add("GetLayerInfo");
            m_Cmds.Add("ConnectionTest");
            return true;
        }


        private List<string> m_Cmds;
        public override void DeInit()
        {

        }

        public override bool DoCmdAsync(CGeoDatabase DB, string Cmd, string SessionID, VarStruct InputParam, int CommandID, CommandStatus Status)
        {
            bool Ret = false;
            switch (Cmd)
            {
                case "ConnectionTest":
                    Ret = ConnectionTest(DB, SessionID, InputParam, Status);
                    break;
            }
            return Ret;
        }

        private bool ConnectionTest(CGeoDatabase dB, string sessionID, VarStruct inputParam, CommandStatus status)
        {
            status.m_Data["success"].Set(false);
            for (int i = 0; i <= 100; i++)
            {
                status.SetProgress(i);
                if (status.isAbort())
                {
                    string msg_aborted = "Connection test was aborted.";
                    status.m_Data["message"].Set(msg_aborted);
                    return true;
                }
            }
            string msg_succeeded = "Connection test succeeded.";
            status.m_Data["message"].Set(msg_succeeded);
            status.m_Data["success"].Set(true);
            return true;
        }

        public override bool DoCmd(CGeoDatabase DB, string Cmd, string SessionID, VarStruct InputParm, out VarStruct RetParm)
        {
            bool Ret = false;
            RetParm = null;
            switch (Cmd)
            {
                case "GetAllLayerInfo":
                    Ret = GetAllLayerInfo(DB, SessionID, out RetParm);
                    break;
                case "GetLayerInfo":
                    Ret = GetLayerInfo(DB, SessionID, InputParm, out RetParm);
                    break;
            }
            return Ret;
        }

        private void RecursiveLayerInfo(CLayer Layer, VarStruct Result)
        {
            Result["islayerset"].Set(Layer.Type == LAYER_TYPE.SET);
            Result["layername"].Set(Layer.Name);
            Result["setting"].Set(Layer.GetSetting());
            if (Layer.Type == LAYER_TYPE.SET)
            {
                int count = ((CLayerSet)Layer).Layers.Count;
                if (count > 0)
                {
                    Result["layers"].CreateArray(count);
                    for (int i = 0; i < count; i++)
                    {
                        VarStruct _Result = (Result["layers"].GetArray())[i].CreateStruct();
                        RecursiveLayerInfo(((CLayerSet)Layer).Layers[i], _Result);
                    }
                }
            }
            else
            {
                Result["epsgcode"].Set(Layer.EPSG);
                Result["type"].Set(Convert.ToInt32(Layer.Type));
                Result["boundary"].Set(Layer.Boundary);
            }
        }

        private bool GetLayerInfo(CGeoDatabase dB, string sessionID, VarStruct inputParm, out VarStruct retParm)
        {
            bool Ret = true;
            retParm = new VarStruct();
            CGroup Group = dB.GetGroup();
            string LayerName = inputParm["layername"].GetString();
            CLayer Layer = null;
            if (Group.IsLayerOK(LayerName))
            {
                Layer = dB.FindLayer(LayerName);
            }
            if (Layer == null)
            {
                dB.FindSessionLayer(LayerName);
            }
            if (Layer != null)
            {
                retParm["success"].Set(true);
                VarStruct Result = new VarStruct();
                RecursiveLayerInfo(Layer, Result);
                retParm["ret"].Set(Result);
            }
            else
            {
                retParm["success"].Set(false);
            }
            return Ret;
        }

        private bool GetAllLayerInfo(CGeoDatabase dB, string sessionID, out VarStruct retParm)
        {
            bool Ret = true;
            retParm = new VarStruct();
            retParm["success"].Set(true);
            CGroup Group = dB.GetGroup();
            int Count = Group.Layers.Count;
            VarArray result = retParm["ret"].CreateArray(Count);
            for (int i = 0; i < Count; i++)
            {
                VarStruct _result = result[i].CreateStruct();
                RecursiveLayerInfo(Group.Layers[i], _result);
            }
            return Ret;
        }

        public override string[] GetSupportCmds()
        {
            return m_Cmds.ToArray();
        }
    }
}
