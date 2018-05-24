using System;
using System.Linq;
using Agebull.ZeroNet.Core;
using Agebull.ZeroNet.ZeroApi;
using Newtonsoft.Json;

namespace ZeroNet.Http.Route
{
    /// <summary>
    ///     管理类
    /// </summary>
    internal class ZeroManager
    {
        private RouteData _data;
        private string[] _words;
        private ApiResult _result;
        public void Command(RouteData data)
        {
            _data = data;
            _words = data.ApiName.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (_words.Length == 0)
            {
                data.Status = RouteStatus.FormalError;
                data.ResultMessage = RouteRuntime.ArgumentErrorJson;
                return;
            }

            switch (_words[0].ToLower())
            {
                case "install":
                    Install();
                    break;
                default:
                    Call();
                    break;
            }
            _data.ResultMessage = JsonConvert.SerializeObject(_result);
        }

        private void Install()
        {
            if (_words.Length < 3)
            {
                _data.Status = RouteStatus.FormalError;
                _result = ApiResult.Error(ErrorCode.ArgumentError);
                return;
            }

            var stationName = _words[2];
            lock (ZeroApplication.Configs)
            {
                if (ZeroApplication.Configs.TryGetValue(stationName, out var config))
                {
                    _result = ApiResult<StationConfig>.Succees(config);
                    _result.Status = new ApiStatsResult
                    {
                        ErrorCode = ErrorCode.Success,
                        ClientMessage = "站点已存在"
                    };
                    return;
                }
            }

            if (ZeroApplication.State != StationState.Run)
            {
                _result = ApiResult.Error(ErrorCode.NoReady);
                return;
            }

            string type = _words[1];
            try
            {
                var result = SystemManager.CallCommand(_words);
                if (!result.InteractiveSuccess)
                {
                    _result = ApiResult.Error(ErrorCode.NetworkError,"服务器无法访问");
                    return;
                }
                switch (result.State)
                {
                    case ZeroStateType.InstallArgumentError:
                        _result = ApiResult.Error(ErrorCode.ArgumentError, $"命令格式错误:{result.State.Text()}");
                        return;
                    case ZeroStateType.NoSupport:
                        _result = ApiResult.Error(ErrorCode.UnknowError, $"类型{type}不支持");
                        return;
                    case ZeroStateType.Failed:
                        _result = ApiResult.Error(ErrorCode.ArgumentError, "已存在");
                        _result.Status = new ApiStatsResult
                        {
                            ErrorCode = ErrorCode.Success,
                            ClientMessage = "安装成功"
                        };
                        return;
                    case ZeroStateType.Ok:
                        _result = ApiResult.Succees();
                        _result.Status = new ApiStatsResult
                        {
                            ErrorCode = ErrorCode.Success,
                            ClientMessage = "安装成功"
                        };
                        return;
                    default:
                        _result = ApiResult.Error(ErrorCode.UnknowError, result.State.Text());
                        return;
                }
            }
            catch (Exception e)
            {
                _result = ApiResult.Error(ErrorCode.NetworkError, e.Message);
            }

            //try
            //{
            //    config = JsonConvert.DeserializeObject<StationConfig>(datas[0]);
            //}
            //catch
            //{
            //    _result = ApiResult.Error(ErrorCode.UnknowError, "返回值不正确");
            //    return;
            //}
            //lock (ZeroApplication.Configs)
            //{
            //    ZeroApplication.Configs.Add(stationName, config);
            //}
            //_result = ApiResult<StationConfig>.Succees(config);
            //_result.Status = new ApiStatsResult
            //{
            //    ErrorCode = ErrorCode.Success,
            //    ClientMessage = "安装成功"
            //};
        }

        private void Call()
        {
            if (_words.Length < 2)
            {
                _data.Status = RouteStatus.FormalError;
                _result = ApiResult.Error(ErrorCode.ArgumentError);
                return;
            }
            
            if (ZeroApplication.State != StationState.Run)
            {
                _result = ApiResult.Error(ErrorCode.NoReady);
                return;
            }

            var value = SystemManager.CallCommand(_words);
            if (!value.InteractiveSuccess)
            {
                _result = ApiResult.Error(ErrorCode.NetworkError);
                return;
            }
            switch (value.State)
            {
                case ZeroStateType.NoSupport:
                    _result = ApiResult.Error(ErrorCode.UnknowError, "不支持的操作");
                    return;
                case ZeroStateType.Ok:
                    _result = ApiValueResult.Succees(value.GetValue(ZeroFrameType.TextValue) ?? value.State.Text());
                    _result.Status = new ApiStatsResult
                    {
                        ErrorCode = ErrorCode.Success,
                        ClientMessage = "操作成功"
                    };
                    return;
                default:
                    _result = ApiResult.Error(ErrorCode.UnknowError, value.State.Text());
                    return;
            }
        }
    }
}