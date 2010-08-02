﻿/* 
 Copyright (c) 2010, NHIN Direct Project
 All rights reserved.

 Authors:
    Umesh Madan     umeshma@microsoft.com
  
Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
Neither the name of the The NHIN Direct Project (nhindirect.org). nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using NHINDirect.Mail;
using NHINDirect.Mime;
using NHINDirect.Certificates;
using NHINDirect.Cryptography;

namespace NHINDirect.Agent
{
	/// <summary>
	/// Master client for mail encryption/decryption and signature management.
	/// </summary>
	/// 
	/// <example>
	/// This example demonstrates a typical use of the Agent, using local certificate management for private certificates,
	/// DNS management for remote certificates, and a local store of trust anchors.
	/// <code>
	/// CertificateIndex localcerts = SystemX509Store.OpenPrivate().Index();
	/// var dnsresolver = new DnsCertResolver("8.8.8.8");
	/// var trustanchors = TrustAnchorResolver.CreateDefault();
	/// var ougoingmsg = File.ReadAllText("outgoing.eml"); // plaintext RFC 5322 email message
	/// var incomingmsg = File.ReadAllText("incoming.eml"); // signed and encrypted S/MIME message
	/// var agent = NHINDAgent("hie.example.com", localcerts, dnsresolver, trustanchors);
	/// 
	/// IncomingMessage incoming = agent.ProcessIncoming(incomingmsg);
	/// OutgoingMessage outgoing = agent.ProcessOutgoing(outgoingmsg);
	/// </code>
	/// </example>
	/// 
    public class NHINDAgent
    {
        SMIMECryptographer m_cryptographer;
        ICertificateResolver m_privateCertResolver;
        ICertificateResolver m_publicCertResolver;
        ITrustAnchorResolver m_trustAnchors;
        TrustModel m_trustModel;
        TrustEnforcementStatus m_minTrustRequirement;
        string m_domain;
        //
        // Options
        //
        bool m_encryptionEnabled = true;
        bool m_wrappingEnabled = true;
        bool m_allowNonWrappedIncoming = true;
        bool m_fetchIncomingSenderCerts = false;
        
		/// <summary>
		/// Creates an NHINDAgent instance using local certificate stores. 
		/// </summary>
		/// <param name="domain">
		/// The local domain name managed by this agent.
		/// </param>
        public NHINDAgent(string domain)
            : this(domain, SystemX509Store.OpenPrivate().Index(), 
                           SystemX509Store.OpenExternal().Index(),
                           TrustAnchorResolver.CreateDefault())
        {
        }
                        
        public NHINDAgent(string domain, ICertificateResolver privateCerts, ICertificateResolver publicCerts, ITrustAnchorResolver anchors)
            : this(domain, privateCerts, publicCerts, anchors, TrustModel.Default, SMIMECryptographer.Default)
        {
        }

        public NHINDAgent(string domain, ICertificateResolver privateCerts, ICertificateResolver publicCerts, ITrustAnchorResolver anchors, TrustModel trustModel, SMIMECryptographer cryptographer)
        {
            if (string.IsNullOrEmpty(domain))
            {
                throw new ArgumentException("domain");
            }
            
            if (privateCerts == null)
            {
                throw new ArgumentNullException("privateCerts");
            }
            if (publicCerts == null)
            {
                throw new ArgumentNullException("publicCerts");
            }
            if (anchors == null)
            {
                throw new ArgumentNullException("anchors");
            }
            if (trustModel == null)
            {
                throw new ArgumentNullException("trustModel");
            }
            if (cryptographer == null)
            {
                throw new ArgumentNullException("cryptographer");
            }
            
            this.m_domain = domain;
            this.m_privateCertResolver = privateCerts;
            this.m_publicCertResolver = publicCerts;
            this.m_cryptographer = cryptographer;
            this.m_trustAnchors = anchors;
            this.m_trustModel = trustModel;
            this.m_minTrustRequirement = TrustEnforcementStatus.Success_Offline;
        }

        public string Domain
        {
            get
            {
                return this.m_domain;
            }
        }

        public SMIMECryptographer Cryptographer
        {
            get
            {
                return this.m_cryptographer;
            }
        }

        public bool EncryptMessages
        {
            get
            {
                return this.m_encryptionEnabled;
            }
            set
            {
                this.m_encryptionEnabled = value;
            }
        }
        
        public bool WrapMessages
        {
            get
            {
                return this.m_wrappingEnabled;
            }
            set
            {
                this.m_wrappingEnabled = value;
            }
        }
        
        public bool AllowNonWrappedIncoming
        {
            get
            {
                return m_allowNonWrappedIncoming;
            }
            set
            {
                m_allowNonWrappedIncoming = value;
            }
        }
        
        public bool FetchSenderCertsIncoming
        {
            get
            {
                return m_fetchIncomingSenderCerts;
            }
            set
            {
                m_fetchIncomingSenderCerts = value;
            }
        }
        
        public ICertificateResolver PublicCertResolver
        {
            get
            {
                return this.m_publicCertResolver;
            }
        }

        public ICertificateResolver PrivateCertResolver
        {
            get
            {
                return this.m_privateCertResolver;
            }
        }

        public ITrustAnchorResolver TrustAnchors
        {
            get
            {
                return this.m_trustAnchors;
            }
        }

        /// <summary>
        /// Messages must satisfy this minimum trust status
        /// </summary>
        public TrustEnforcementStatus MinTrustRequirement
        {
            get
            {
                return this.m_minTrustRequirement;
            }
            set
            {
                if (value < TrustEnforcementStatus.Success_Offline)
                {
                    throw new ArgumentException();
                }
                this.m_minTrustRequirement = value;
            }
        }

        //
        // You can participate in the agent pipeline by subscribing to these events
        // You can choose to do FURTHER post-processing on the message:
        //   - adding headers
        //   - throwing exceptions
        // If you throw an exception, message processing is ABORTED
        //
        public event Action<NHINDAgent, Exception> Error;
        public event Action<IncomingMessage> PreProcessIncoming;
        public event Action<IncomingMessage> PostProcessIncoming;
        public event Action<IncomingMessage, Exception> ErrorIncoming;
        public event Action<OutgoingMessage> PreProcessOutgoing;
        public event Action<OutgoingMessage> PostProcessOutgoing;
        public event Action<OutgoingMessage, Exception> ErrorOutgoing;
        
        /// <summary>
        /// If the message is encrypted, then treat it as an incoming message
        /// Else treat it as an outgoing message
        /// You'll need to cast MessageEnvelope to IncomingMessage/OutgoingMessage
        /// </summary>
        public MessageEnvelope Process(string messageText, ref bool isIncoming)
        {
            Message message = MimeSerializer.Default.Deserialize<Message>(messageText);
            if (SMIMEStandard.IsEncrypted(message))
            {
                isIncoming = true;
                IncomingMessage incoming = new IncomingMessage(message);
                this.ProcessIncoming(incoming);
                return incoming;
            }

            isIncoming = false;
            OutgoingMessage outgoing = new OutgoingMessage(message, messageText);
            this.ProcessOutgoing(outgoing);
            return outgoing;
        }
        
        public MessageEnvelope Process(string messageText, NHINDAddressCollection recipients, NHINDAddress sender, ref bool isIncoming)
        {
            this.CheckEnvelopeAddresses(recipients, sender);

            Message message = MimeSerializer.Default.Deserialize<Message>(messageText);            
            if (SMIMEStandard.IsEncrypted(message))
            {
                isIncoming = true;
                IncomingMessage incoming = new IncomingMessage(message, recipients, sender);
                this.ProcessIncoming(incoming);
                return incoming;
            }

            isIncoming = false;
            OutgoingMessage outgoing = new OutgoingMessage(message, messageText, recipients, sender);
            this.ProcessOutgoing(outgoing);
            return outgoing;
        }
                      
        //-------------------------------------------------------------------
        //
        // INCOMING MESSAGE
        //
        //-------------------------------------------------------------------
        public IncomingMessage ProcessIncoming(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                throw new ArgumentException();
            }

            return this.ProcessIncoming(new IncomingMessage(
                                    MimeSerializer.Default.Deserialize<Message>(messageText))
                                    );
        }

        public IncomingMessage ProcessIncoming(string messageText, NHINDAddressCollection recipients, NHINDAddress sender)
        {
            this.CheckEnvelopeAddresses(recipients, sender);
            
            IncomingMessage message = new IncomingMessage(
                                    MimeSerializer.Default.Deserialize<Message>(messageText),
                                    recipients,
                                    sender
                                    );

            return this.ProcessIncoming(message);                    
        }
        
        public IncomingMessage ProcessIncoming(IncomingMessage message)
        {
            if (message == null)
            {
                throw new ArgumentException();
            }

            message.Agent = this;
            try
            {
                message.Validate();

                this.Notify(message, this.PreProcessIncoming);

                this.ProcessMessage(message);

                this.Notify(message, this.PostProcessIncoming);
            }
            catch (Exception error)
            {
                this.Notify(message, error);
                throw;  // rethrow error
            }
            
            return message;
        }

        void ProcessMessage(IncomingMessage message)
        {
            if (message.Sender == null)
            {
                throw new AgentException(AgentError.UntrustedSender);
            }

            message.CategorizeRecipients(this.Domain);
            if (!message.HasDomainRecipients)
            {
                throw new AgentException(AgentError.NoTrustedRecipients);
            }
            //
            // Map each address to its certificates/trust settings
            //
            this.BindAddresses(message);
            //
            // Decrypt the message, extract the signature and original content
            //
            this.DecryptSignedContent(message);
            //
            // The standard requires that the original message be wrapped to protect headers
            //
            message.Message = this.UnwrapMessage(message.Message);
            //
            // Enforce trust requirements, including checking signatures
            //
            this.m_trustModel.Enforce(message);
            //
            // Remove any untrusted recipients...
            //
            if (message.HasDomainRecipients)
            {
                message.CategorizeRecipients(this.m_minTrustRequirement);
            }
            if (!message.HasDomainRecipients)
            {
                throw new AgentException(AgentError.NoTrustedRecipients);
            }
            //
            // Some recipients may not trust this message. Remove them from the To list to prevent accidental message delivery
            //
            message.UpdateRoutingHeaders();
        }

        void BindAddresses(IncomingMessage message)
        {
            if (m_fetchIncomingSenderCerts)
            {
                //
                // Retrieving the sender's certificate is optional
                //
                message.Sender.Certificates = this.ResolvePublicCerts(message.Sender, false);
            }
            //
            // Bind each recpient's certs and trust settings
            //
            NHINDAddressCollection recipients = message.DomainRecipients;
            for (int i = 0, count = recipients.Count; i < count; ++i)
            {
                NHINDAddress recipient = recipients[i];
                recipient.Certificates = this.ResolvePrivateCerts(recipient, false);
                recipient.TrustAnchors = this.m_trustAnchors.IncomingAnchors.GetCertificates(recipient);
            }
        }

        void DecryptSignedContent(IncomingMessage message)
        {   
            MimeEntity decryptedEntity = this.DecryptMessage(message);
            SignedCms signatures;
            MimeEntity payload;
            
            if (SMIMEStandard.IsContentEnvelopedSignature(decryptedEntity.ParsedContentType))
            {
                signatures = m_cryptographer.DeserializeEnvelopedSignature(decryptedEntity);                
                payload = MimeSerializer.Default.Deserialize<MimeEntity>(signatures.ContentInfo.Content);
            }                        
            else if (SMIMEStandard.IsContentMultipartSignature(decryptedEntity.ParsedContentType))
            {
                SignedEntity signedEntity = SignedEntity.Load(decryptedEntity);                
                signatures = m_cryptographer.DeserializeDetachedSignature(signedEntity);
                payload = signedEntity.Content; 
            }
            else
            {
                throw new AgentException(AgentError.UnsignedMessage);
            }
            
            message.Signatures = signatures;
            //
            // Alter body to contain actual content. Also clean up mime headers on the message that were there to support
            // signatures etc
            //
            HeaderCollection headers = message.Message.Headers;
            message.Message.Headers = headers.SelectNonMimeHeaders();
            message.Message.ApplyBody(payload); // this will merge in content + content specific mime headers
        }

        MimeEntity DecryptMessage(IncomingMessage message)
        {
            MimeEntity decryptedEntity = null;
            if (this.m_encryptionEnabled)
            {
                //
                // Yes, this can be optimized heavily for multiple certs. 
                // But we will start with the easy to understand simple version
                //            
                // Decrypt and parse message body into a signature entity - the envelope that contains our data + signature
                // We can use the cert of any ONE of the recipients to decrypt
                // So basically, we'll try until we find one, or we just run out...
                //
                foreach (X509Certificate2 cert in message.DomainRecipients.Certificates)
                {
                    try
                    {
                        decryptedEntity = this.m_cryptographer.Decrypt(message.Message, cert);
                        break;
                    }
                    catch
                    {
                    }
                }
            }
            else
            {
                decryptedEntity = message.Message;
            }
            
            if (decryptedEntity == null)
            {
                throw new AgentException(AgentError.UntrustedMessage);
            }

            return decryptedEntity;
        }

        //-------------------------------------------------------------------
        //
        // OUTGOING MESSAGE
        //
        //-------------------------------------------------------------------        
        public OutgoingMessage ProcessOutgoing(string messageText)
        {
            if (string.IsNullOrEmpty(messageText))
            {
                throw new ArgumentException();
            }

            OutgoingMessage message = new OutgoingMessage(this.WrapMessage(messageText));
            
            return this.ProcessOutgoing(message);
        }
        
        public OutgoingMessage ProcessOutgoing(string messageText, NHINDAddressCollection recipients, NHINDAddress sender)
        {
            this.CheckEnvelopeAddresses(recipients, sender);

            OutgoingMessage message = new OutgoingMessage(
                                    this.WrapMessage(messageText),
                                    recipients,
                                    sender
                                    );
            
            return this.ProcessOutgoing(message);            
        }
        
        public OutgoingMessage ProcessOutgoing(OutgoingMessage message)
        {
            if (message == null)
            {
                throw new ArgumentException();
            }

            message.Agent = this;            
            try
            {
                message.Validate();

                this.Notify(message, this.PreProcessOutgoing);

                this.ProcessMessage(message);

                this.Notify(message, this.PostProcessOutgoing);
            }
            catch (Exception error)
            {
                this.Notify(message, error);
                throw;
            }
            
            return message;
        }

        void ProcessMessage(OutgoingMessage message)
        {
            if (!WrappedMessage.IsWrapped(message.Message))
            {
                message.Message = message.HasRawMessage ? this.WrapMessage(message.RawMessage) : message.Message;
            }
            
            if (message.Sender == null)
            {
                throw new AgentException(AgentError.MissingFrom);
            }
            
            this.BindAddresses(message);
            if (!message.HasRecipients)
            {
                throw new AgentException(AgentError.MissingTo);
            }            
            message.CategorizeRecipients(m_domain);
            //
            // Enforce the trust model.
            //
            this.m_trustModel.Enforce(message);
            //
            // Remove any non-trusted recipients
            //
            message.CategorizeRecipients(this.m_minTrustRequirement);
            if (!message.HasRecipients)
            {
                throw new AgentException(AgentError.NoTrustedRecipients);
            }
            //
            // Finally, sign and encrypt the message
            //
            this.SignAndEncryptMessage(message);
            //
            // Not all recipients may be trusted. Remove them from Routing headers
            //
            message.UpdateRoutingHeaders();
        }

        void BindAddresses(OutgoingMessage message)
        {
            //
            // Retrieving the sender's private certificate is requied for encryption
            //
            message.Sender.TrustAnchors = this.m_trustAnchors.OutgoingAnchors.GetCertificates(message.Sender);
            message.Sender.Certificates = this.ResolvePrivateCerts(message.Sender, true);
            //
            // Bind each recipient's certs
            //
            NHINDAddressCollection recipients = message.Recipients;
            for (int i = 0, count = recipients.Count; i < count; ++i)
            {
                NHINDAddress recipient = recipients[i];
                recipient.Certificates = this.ResolvePublicCerts(recipient, false);
            }
        }
        
        Message WrapMessage(string messageText)
        {
            if (!m_wrappingEnabled)
            {
                return MimeSerializer.Default.Deserialize<Message>(messageText);
            }
            
            return WrappedMessage.Create(messageText, NHINDStandard.MailHeadersUsed);            
        }

        Message WrapMessage(Message message)
        {
            if (!m_wrappingEnabled)
            {
                return message;
            }

            return WrappedMessage.Create(message, NHINDStandard.MailHeadersUsed);
        }
        
        Message UnwrapMessage(Message message)
        {
            if (!m_wrappingEnabled)
            {
                return message;
            }
            
            if (m_allowNonWrappedIncoming && !WrappedMessage.IsWrapped(message))
            {
                return message;
            }
            
            return WrappedMessage.ExtractInner(message);
        }
        //
        // First sign, THEN encrypt the message
        //
        void SignAndEncryptMessage(OutgoingMessage message)
        {
            SignedEntity signedEntity = this.m_cryptographer.Sign(message.Message, message.Sender.Certificates);

            if (this.m_encryptionEnabled)
            {
                //
                // Encrypt the outbound message with all known trusted certs
                //
                MimeEntity encryptedEntity = this.m_cryptographer.Encrypt(signedEntity, message.Recipients.GetCertificates());
                //
                // Alter message content to contain encrypted data
                //
                message.Message.ApplyBody(encryptedEntity);
            }
            else
            {
                message.Message.ApplyBody(signedEntity);
            }
        }

        X509Certificate2Collection ResolvePrivateCerts(MailAddress address, bool required)
        {
            X509Certificate2Collection certs = null;
            try
            {
                certs = this.m_privateCertResolver.GetCertificates(address);
                if (certs == null && required)
                {
                    throw new AgentException(AgentError.UnknownRecipient);
                }
            }
            catch (Exception ex)
            {
                if (required)
                {
                    throw;
                }
                this.Notify(ex); // for logging, tracking etc...
            }

            return certs;
        }

        X509Certificate2Collection ResolvePublicCerts(MailAddress address, bool required)
        {
            X509Certificate2Collection cert = null;
            try
            {
                cert = this.m_publicCertResolver.GetCertificates(address);
                if (cert == null && required)
                {
                    throw new AgentException(AgentError.UnknownRecipient);
                }
            }
            catch (Exception ex)
            {
                if (required)
                {
                    throw;
                }
                this.Notify(ex); // for logging, tracking etc...
            }

            return cert;
        }
        
        void CheckEnvelopeAddresses(NHINDAddressCollection recipients, NHINDAddress sender)
        {
            if (recipients == null || recipients.Count == 0)
            {
                throw new AgentException(AgentError.NoRecipients);
            }
            if (sender == null)
            {
                throw new AgentException(AgentError.NoSender);
            }
            
            recipients.SetSource(AddressSource.RcptTo);
            sender.Source = AddressSource.MailFrom;
        }
                
        void Notify(IncomingMessage message, Action<IncomingMessage> eventHandler)
        {
            //
            // exceptions are interpreted as: abort message
            //
            if (eventHandler != null)
            {
                eventHandler(message);
            }
        }

        void Notify(OutgoingMessage message, Action<OutgoingMessage> eventHandler)
        {
            //
            // exceptions are interpreted as: abort message
            //
            if (eventHandler != null)
            {
                eventHandler(message);
            }
        }

        void Notify(IncomingMessage message, Exception ex)
        {
            try
            {
                if (this.ErrorIncoming != null)
                {
                    this.ErrorIncoming(message, ex);
                }
            }
            catch
            {
            }
        }

        void Notify(OutgoingMessage message, Exception ex)
        {
            try
            {
                if (this.ErrorOutgoing != null)
                {
                    this.ErrorOutgoing(message, ex);
                }
            }
            catch
            {
            }
        }

        void Notify(Exception ex)
        {
            try
            {
                if (this.Error != null)
                {
                    this.Error(this, ex);
                }
            }
            catch
            {
            }
        }
    }
}
