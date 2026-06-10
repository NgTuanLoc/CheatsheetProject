import { LoginForm } from '@/components/auth/LoginForm';

export default function LoginPage() {
  return (
    <main className="min-h-screen flex items-center justify-center">
      <div className="glass p-8 w-full max-w-sm space-y-6">
        <h1 className="text-2xl font-bold text-center">Cheatsheets</h1>
        <LoginForm />
      </div>
    </main>
  );
}
