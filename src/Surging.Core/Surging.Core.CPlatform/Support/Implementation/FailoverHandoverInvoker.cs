﻿using Surging.Core.CPlatform.Convertibles;
using Surging.Core.CPlatform.Exceptions;
using Surging.Core.CPlatform.Messages;
using Surging.Core.CPlatform.Runtime.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Surging.Core.CPlatform.Support.Implementation
{
    public class FailoverHandoverInvoker : IClusterInvoker
    {
        #region Field
        private readonly IRemoteInvokeService _remoteInvokeService;
        private readonly ITypeConvertibleService _typeConvertibleService;
        private readonly IBreakeRemoteInvokeService _breakeRemoteInvokeService;
        private readonly IServiceCommandProvider _commandProvider;
        #endregion Field

        #region Constructor

        public FailoverHandoverInvoker(IRemoteInvokeService remoteInvokeService, IServiceCommandProvider commandProvider,
            ITypeConvertibleService typeConvertibleService, IBreakeRemoteInvokeService breakeRemoteInvokeService)
        {
            _remoteInvokeService = remoteInvokeService;
            _typeConvertibleService = typeConvertibleService;
            _breakeRemoteInvokeService = breakeRemoteInvokeService;
            _commandProvider = commandProvider;
        }

        #endregion Constructor

        public async Task<T> Invoke<T>(IDictionary<string, object> parameters, string serviceId, string _serviceKey, bool decodeJOject)
        {
            var time = 0;
            T result = default(T);
            RemoteInvokeResultMessage message = null;
            var vtCommand = _commandProvider.GetCommand(serviceId);
            var command = vtCommand.IsCompletedSuccessfully ? vtCommand.Result : await vtCommand;
            do
            {
                message = await _breakeRemoteInvokeService.InvokeAsync(parameters, serviceId, _serviceKey, decodeJOject);

                if (message != null && message.Result != null)
                {
                    if (message.StatusCode != StatusCode.Success && time >= command.FailoverCluster)
                    {
                        throw new CPlatformException(message.ExceptionMessage, message.StatusCode);
                    }
                    if (message.Result != null) 
                    {
                        result = (T)_typeConvertibleService.Convert(message.Result, typeof(T));
                    }                    
                }
            } while ((message == null || message.StatusCode == StatusCode.ServiceUnavailability) && ++time < command.FailoverCluster);
            return result;
        }

        public async Task Invoke(IDictionary<string, object> parameters, string serviceId, string _serviceKey, bool decodeJOject)
        {
            var time = 0;
            var command = await _commandProvider.GetCommand(serviceId);
            RemoteInvokeResultMessage message = null;
            do
            {
                message = await _breakeRemoteInvokeService.InvokeAsync(parameters, serviceId, _serviceKey, decodeJOject);
                if (message != null && message.Result != null)
                {
                    if (message.StatusCode != StatusCode.Success && time >= command.FailoverCluster)
                    {
                        throw new CPlatformException(message.ExceptionMessage, message.StatusCode);
                    }
                }
            }
            while ((message == null || message.StatusCode == StatusCode.ServiceUnavailability) && ++time < command.FailoverCluster);
        }

        public async Task<object> Invoke(IDictionary<string, object> parameters, Type returnType, string serviceId, string _serviceKey, bool decodeJOject)
        {
            var time = 0;
            object result = null;
            RemoteInvokeResultMessage message = null;
            var vtCommand = _commandProvider.GetCommand(serviceId);
            var command = vtCommand.IsCompletedSuccessfully ? vtCommand.Result : await vtCommand;
            do
            {
                message = await _breakeRemoteInvokeService.InvokeAsync(parameters, serviceId, _serviceKey, decodeJOject);

                if (message != null && message.Result != null)
                {
                    if (message.StatusCode != StatusCode.Success && time >= command.FailoverCluster)
                    {
                        throw new CPlatformException(message.ExceptionMessage, message.StatusCode);
                    }
                    if (message.Result != null)
                    {
                        result = _typeConvertibleService.Convert(message.Result, returnType);
                    }
                    
                }
            } while ((message == null || message.StatusCode == StatusCode.ServiceUnavailability) && ++time < command.FailoverCluster);
            return result;
        }
    }

}
