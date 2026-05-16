'use client';

import { useState } from 'react';
import { signIn } from 'next-auth/react';
import { useRouter } from 'next/navigation';
import Image from 'next/image';
import { Loader2 } from 'lucide-react';

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState('');
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    setError('');

    const result = await signIn('credentials', {
      email,
      password,
      redirect: false,
    });

    if (result?.error) {
      setError('Invalid credentials or insufficient permissions');
      setIsLoading(false);
      return;
    }

    router.push('/');
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-[#0F172A]">
      <div className="w-full max-w-md space-y-8 px-6">
        <div className="text-center">
          <div className="flex justify-center mb-4">
            <div className="flex h-16 w-16 sm:h-20 sm:w-20 items-center justify-center rounded-2xl bg-emerald-500/10 p-3">
              <Image
                src="/logo.svg"
                alt="Gym 360"
                width={64}
                height={64}
                priority
                className="h-full w-full object-contain"
              />
            </div>
          </div>
          <h1 className="text-2xl font-bold text-white font-heading">Gym 360 Admin</h1>
          <p className="mt-2 text-sm text-slate-400">Sign in to the platform administration dashboard</p>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4">
          {error && (
            <div className="rounded-lg bg-red-500/10 border border-red-500/20 px-4 py-3 text-sm text-red-400">
              {error}
            </div>
          )}
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="admin@gymapp.com"
              required
              className="w-full h-10 rounded-lg border border-slate-600 bg-slate-800/50 px-3 text-sm text-white placeholder:text-slate-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-300 mb-1.5">Password</label>
            <input
              type="password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Enter your password"
              required
              className="w-full h-10 rounded-lg border border-slate-600 bg-slate-800/50 px-3 text-sm text-white placeholder:text-slate-500 focus:border-emerald-500 focus:ring-1 focus:ring-emerald-500 outline-none"
            />
          </div>
          <button
            type="submit"
            disabled={isLoading}
            className="w-full h-10 rounded-lg bg-emerald-600 text-sm font-medium text-white hover:bg-emerald-700 disabled:opacity-50 flex items-center justify-center gap-2"
          >
            {isLoading ? <Loader2 className="h-4 w-4 animate-spin" /> : null}
            {isLoading ? 'Signing in...' : 'Sign In'}
          </button>
        </form>

        <p className="text-center text-xs text-slate-500">
          Platform administrators only. Contact super admin for access.
        </p>
      </div>
    </div>
  );
}
