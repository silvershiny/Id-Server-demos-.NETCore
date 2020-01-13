﻿using Fido2NetLib;
using IdentityServer.Data;
using IdentityServer.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.Services
{
    public class Fido2Storage
    {
        private readonly IdentityDbContext _identityDbContext;

        public Fido2Storage(IdentityDbContext identityDbContext)
        {
            _identityDbContext = identityDbContext;
        }

        public async Task<List<FidoStoredCredential>> GetCredentialsByUsername(string username)
        {
            return await _identityDbContext.FidoStoredCredential.Where(c => c.Username == username).ToListAsync();
        }

        public async Task RemoveCredentialsByUsername(string username)
        {
            var item = await _identityDbContext.FidoStoredCredential.Where(c => c.Username == username).FirstOrDefaultAsync();
            if (item != null)
            {
                _identityDbContext.FidoStoredCredential.Remove(item);
                await _identityDbContext.SaveChangesAsync();
            }
        }

        public async Task<FidoStoredCredential> GetCredentialById(byte[] id)
        {
            var credentialIdString = Base64Url.Encode(id);
            //byte[] credentialIdStringByte = Base64Url.Decode(credentialIdString);

            var cred = await _identityDbContext.FidoStoredCredential
                .Where(c => c.DescriptorJson.Contains(credentialIdString)).FirstOrDefaultAsync();

            return cred;
        }

        public Task<List<FidoStoredCredential>> GetCredentialsByUserHandleAsync(byte[] userHandle)
        {
            return Task.FromResult(_identityDbContext.FidoStoredCredential.Where(c => c.UserHandle.SequenceEqual(userHandle)).ToList());
        }

        public async Task UpdateCounter(byte[] credentialId, uint counter)
        {
            var credentialIdString = Base64Url.Encode(credentialId);
            //byte[] credentialIdStringByte = Base64Url.Decode(credentialIdString);

            var cred = await _identityDbContext.FidoStoredCredential
                .Where(c => c.DescriptorJson.Contains(credentialIdString)).FirstOrDefaultAsync();

            cred.SignatureCounter = counter;
            await _identityDbContext.SaveChangesAsync();
        }

        public async Task AddCredentialToUser(Fido2User user, FidoStoredCredential credential)
        {
            credential.UserId = user.Id;
            _identityDbContext.FidoStoredCredential.Add(credential);
            await _identityDbContext.SaveChangesAsync();
        }

        public async Task<List<Fido2User>> GetUsersByCredentialIdAsync(byte[] credentialId)
        {
            var credentialIdString = Base64Url.Encode(credentialId);
            //byte[] credentialIdStringByte = Base64Url.Decode(credentialIdString);

            var cred = await _identityDbContext.FidoStoredCredential
                .Where(c => c.DescriptorJson.Contains(credentialIdString)).FirstOrDefaultAsync();

            if (cred == null)
            {
                return new List<Fido2User>();
            }

            return await _identityDbContext.Users
                    .Where(u => Encoding.UTF8.GetBytes(u.UserName)
                    .SequenceEqual(cred.UserId))
                    .Select(u => new Fido2User
                    {
                        DisplayName = u.UserName,
                        Name = u.UserName,
                        Id = Encoding.UTF8.GetBytes(u.UserName) // byte representation of userID is required
                    }).ToListAsync();
        }
    }
}
