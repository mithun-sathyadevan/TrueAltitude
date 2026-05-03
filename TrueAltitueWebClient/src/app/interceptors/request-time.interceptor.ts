import { HttpInterceptorFn } from '@angular/common/http';
import { from, switchMap } from 'rxjs';

import { environment } from '../../environments/environment';

const REQUEST_TIME_HEADER = 'X-Request-Time';

function toBase64(bytes: Uint8Array): string {
  let binary = '';
  for (const b of bytes) {
    binary += String.fromCharCode(b);
  }
  return btoa(binary);
}

async function deriveAesKey(sharedKey: string): Promise<CryptoKey> {
  const encoder = new TextEncoder();
  const keyMaterial = await crypto.subtle.digest('SHA-256', encoder.encode(sharedKey));
  return crypto.subtle.importKey('raw', keyMaterial, { name: 'AES-GCM' }, false, ['encrypt']);
}

async function encryptTimestamp(sharedKey: string): Promise<string> {
  const timestamp = Date.now().toString();
  const iv = crypto.getRandomValues(new Uint8Array(12));
  const encoder = new TextEncoder();
  const key = await deriveAesKey(sharedKey);
  const encrypted = await crypto.subtle.encrypt(
    { name: 'AES-GCM', iv },
    key,
    encoder.encode(timestamp)
  );

  const cipherBytes = new Uint8Array(encrypted);
  return `${toBase64(iv)}.${toBase64(cipherBytes)}`;
}

export const requestTimeInterceptor: HttpInterceptorFn = (req, next) => {
  const isApiRequest = req.url.startsWith('api/') || req.url.includes('/api/');
  if (!isApiRequest) {
    return next(req);
  }

  return from(encryptTimestamp(environment.requestTimeSharedKey)).pipe(
    switchMap((requestTimeToken) => {
      const securedReq = req.clone({
        setHeaders: {
          [REQUEST_TIME_HEADER]: requestTimeToken,
        },
      });
      return next(securedReq);
    })
  );
};