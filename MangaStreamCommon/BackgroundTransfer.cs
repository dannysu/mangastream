using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using Microsoft.Phone.BackgroundTransfer;

namespace MangaStreamCommon
{
    public class BackgroundTransfer
    {
        // Stores pending download requests to be queued up with BackgroundTransferService
        private List<BackgroundTransferRequest> _requests;

        // Stores callback mapping based on a given tag
        private Dictionary<string, OnTransferCompleted> _idMapping;

        public delegate void OnTransferCompleted(BackgroundTransferRequest request);

        public BackgroundTransfer()
        {
            this._requests = new List<BackgroundTransferRequest>();

            this._idMapping = new Dictionary<string, OnTransferCompleted>();
        }

        public void Serialize()
        {
        }

        public void Deserialize()
        {
            // TODO: Upon deserializing, the class should attempt to add the queue to BackgroundTransferService
        }

        public void QueueDownload(string source, string destination, string tag, OnTransferCompleted callback)
        {
            Uri requestUri = new Uri(source, UriKind.Absolute);
            Uri downloadLocation = new Uri(destination, UriKind.Relative);
            BackgroundTransferRequest request = new BackgroundTransferRequest(requestUri, downloadLocation);
            request.TransferPreferences = TransferPreferences.AllowCellularAndBattery;

            request.TransferStatusChanged += new EventHandler<BackgroundTransferEventArgs>(OnBackgroundTransferStatusChanged);
            request.Tag = tag;

            try
            {
                _idMapping.Add(request.RequestId, callback);
                BackgroundTransferService.Add(request);
            }
            catch (InvalidOperationException)
            {
                _requests.Add(request);
            }
        }

        void OnBackgroundTransferStatusChanged(object sender, BackgroundTransferEventArgs e)
        {
            if (e.Request.TransferStatus == TransferStatus.Completed)
            {
                BackgroundTransferService.Remove(e.Request);

                if (_idMapping.ContainsKey(e.Request.RequestId))
                {
                    _idMapping[e.Request.RequestId].Invoke(e.Request);
                    _idMapping.Remove(e.Request.RequestId);
                }

                // Check if there are pending downloads, if there are then queue them up with background transfer service now.
                if (_requests.Count > 0)
                {
                    try
                    {
                        BackgroundTransferService.Add(_requests[0]);
                        _requests.RemoveAt(0);
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
        }
    }
}
