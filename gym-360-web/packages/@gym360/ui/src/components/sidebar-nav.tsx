'use client';

import { useState, type ReactNode } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '../lib/utils';
import { ChevronLeft, ChevronRight, LogOut, Menu, X } from 'lucide-react';

export interface NavItem {
  label: string;
  href: string;
  icon: ReactNode;
  badge?: number;
}

export interface NavSection {
  label: string;
  items: NavItem[];
}

interface SidebarNavProps {
  sections: NavSection[];
  logo: ReactNode;
  logoCollapsed?: ReactNode;
  userAvatar?: ReactNode;
  userName?: string;
  userRole?: string;
  onSignOut?: () => void;
  variant?: 'admin' | 'staff';
  className?: string;
  defaultCollapsed?: boolean;
  footer?: ReactNode;
}

export function SidebarNav({
  sections,
  logo,
  logoCollapsed,
  userAvatar,
  userName,
  userRole,
  onSignOut,
  variant = 'admin',
  className,
  defaultCollapsed = false,
  footer,
}: SidebarNavProps) {
  const [collapsed, setCollapsed] = useState(defaultCollapsed);
  const pathname = usePathname();

  const isActive = (href: string): boolean => {
    if (href === '/') return pathname === '/';
    return pathname === href || pathname.startsWith(href + '/');
  };

  const activeBorderColor = variant === 'admin' ? 'bg-emerald-500' : 'bg-[#C41E3A]';
  const activeIconColor = variant === 'admin' ? 'text-emerald-500' : 'text-[#C41E3A]';
  const activeRowBg = variant === 'admin' ? 'bg-emerald-500/10' : 'bg-[#C41E3A]/10';
  const badgeBg = variant === 'admin' ? 'bg-emerald-500' : 'bg-[#C41E3A]';

  return (
    <aside
      className={cn(
        'fixed inset-y-0 left-0 z-30 flex flex-col border-r transition-all duration-300',
        variant === 'admin'
          ? 'bg-[#0F172A] border-slate-700/50 text-slate-300'
          : 'bg-background border-border text-muted-foreground',
        collapsed ? 'w-[72px]' : 'w-[260px]',
        className
      )}
    >
      {/* Logo */}
      <div
        className={cn(
          'flex items-center gap-3 px-4 h-16 border-b shrink-0',
          variant === 'admin' ? 'border-slate-700/50' : 'border-border'
        )}
      >
        {collapsed ? (logoCollapsed ?? logo) : logo}
        <button
          onClick={() => setCollapsed(!collapsed)}
          className={cn(
            'ml-auto p-1.5 rounded-lg transition-colors',
            variant === 'admin'
              ? 'hover:bg-slate-700/50 text-slate-400 hover:text-slate-200'
              : 'hover:bg-accent text-muted-foreground hover:text-foreground'
          )}
        >
          {collapsed ? <Menu className="h-4 w-4" /> : <X className="h-4 w-4" />}
        </button>
      </div>

      {/* Navigation */}
      <nav className="flex-1 overflow-y-auto py-4 px-2">
        {sections.map((section) => (
          <div key={section.label} className="mb-6">
            {!collapsed && (
              <p
                className={cn(
                  'text-[10px] font-semibold uppercase tracking-wider px-3 mb-2',
                  variant === 'admin' ? 'text-slate-500' : 'text-muted-foreground'
                )}
              >
                {section.label}
              </p>
            )}
            <div className="space-y-0.5">
              {section.items.map((item) => {
                const active = isActive(item.href);
                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    className={cn(
                      'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all relative group',
                      active
                        ? cn(activeRowBg, variant === 'admin' ? 'text-white' : 'text-foreground')
                        : variant === 'admin'
                          ? 'text-slate-400 hover:text-slate-200 hover:bg-slate-700/30'
                          : 'text-muted-foreground hover:text-foreground hover:bg-accent'
                    )}
                  >
                    {active && (
                      <div
                        className={cn(
                          'absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-5 rounded-r-full',
                          activeBorderColor
                        )}
                      />
                    )}
                    <span className={cn('shrink-0', active && activeIconColor)}>
                      {item.icon}
                    </span>
                    {!collapsed && (
                      <>
                        <span className="flex-1 truncate">{item.label}</span>
                        {item.badge !== undefined && item.badge > 0 && (
                          <span
                            className={cn(
                              'text-[10px] font-bold text-white px-1.5 py-0.5 rounded-full min-w-[18px] text-center',
                              badgeBg
                            )}
                          >
                            {item.badge}
                          </span>
                        )}
                      </>
                    )}
                    {collapsed && item.badge !== undefined && item.badge > 0 && (
                      <span
                        className={cn(
                          'absolute -top-1 -right-1 text-[9px] font-bold text-white w-4 h-4 rounded-full flex items-center justify-center',
                          badgeBg
                        )}
                      >
                        {item.badge}
                      </span>
                    )}
                  </Link>
                );
              })}
            </div>
          </div>
        ))}
      </nav>

      {/* Footer / User */}
      <div
        className={cn(
          'border-t p-3 shrink-0',
          variant === 'admin' ? 'border-slate-700/50' : 'border-border'
        )}
      >
        {footer}
        {userName && (
          <div className="flex items-center gap-3">
            {userAvatar ?? (
              <div
                className={cn(
                  'h-9 w-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0',
                  variant === 'admin'
                    ? 'bg-emerald-500/20 text-emerald-400'
                    : 'bg-[#C41E3A]/10 text-[#C41E3A]'
                )}
              >
                {userName
                  .split(' ')
                  .map((n) => n[0])
                  .join('')
                  .toUpperCase()
                  .slice(0, 2)}
              </div>
            )}
            {!collapsed && (
              <>
                <div className="flex-1 min-w-0">
                  <p
                    className={cn(
                      'text-sm font-medium truncate',
                      variant === 'admin' ? 'text-white' : 'text-foreground'
                    )}
                  >
                    {userName}
                  </p>
                  {userRole && (
                    <p
                      className={cn(
                        'text-xs truncate capitalize',
                        variant === 'admin' ? 'text-slate-400' : 'text-muted-foreground'
                      )}
                    >
                      {userRole.replace(/_/g, ' ')}
                    </p>
                  )}
                </div>
                {onSignOut && (
                  <button
                    onClick={onSignOut}
                    className={cn(
                      'p-1.5 rounded-lg transition-colors',
                      variant === 'admin'
                        ? 'text-slate-500 hover:text-red-400 hover:bg-slate-700/50'
                        : 'text-muted-foreground hover:text-destructive hover:bg-accent'
                    )}
                  >
                    <LogOut className="h-4 w-4" />
                  </button>
                )}
              </>
            )}
          </div>
        )}
      </div>
    </aside>
  );
}
