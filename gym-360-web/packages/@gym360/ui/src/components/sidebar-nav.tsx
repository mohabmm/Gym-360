'use client';

import { useState, type ReactNode } from 'react';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { cn } from '../lib/utils';
import { LogOut, Menu, X } from 'lucide-react';

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

type SidebarVariant = NonNullable<SidebarNavProps['variant']>;

/** Per-variant class names: admin = dark slate + emerald accent, staff = theme surface + brand red. */
const THEMES = {
  admin: {
    aside: 'bg-[#0F172A] border-slate-700/50 text-slate-300',
    divider: 'border-slate-700/50',
    toggle: 'hover:bg-slate-700/50 text-slate-400 hover:text-slate-200',
    sectionLabel: 'text-slate-500',
    activeRow: cn('bg-emerald-500/10', 'text-white'),
    idleRow: 'text-slate-400 hover:text-slate-200 hover:bg-slate-700/30',
    activeBorder: 'bg-emerald-500',
    activeIcon: 'text-emerald-500',
    badge: 'bg-emerald-500',
    avatar: 'bg-emerald-500/20 text-emerald-400',
    userName: 'text-white',
    userRole: 'text-slate-400',
    signOut: 'text-slate-500 hover:text-red-400 hover:bg-slate-700/50',
  },
  staff: {
    aside: 'bg-background border-border text-muted-foreground',
    divider: 'border-border',
    toggle: 'hover:bg-accent text-muted-foreground hover:text-foreground',
    sectionLabel: 'text-muted-foreground',
    activeRow: cn('bg-[#C41E3A]/10', 'text-foreground'),
    idleRow: 'text-muted-foreground hover:text-foreground hover:bg-accent',
    activeBorder: 'bg-[#C41E3A]',
    activeIcon: 'text-[#C41E3A]',
    badge: 'bg-[#C41E3A]',
    avatar: 'bg-[#C41E3A]/10 text-[#C41E3A]',
    userName: 'text-foreground',
    userRole: 'text-muted-foreground',
    signOut: 'text-muted-foreground hover:text-destructive hover:bg-accent',
  },
} satisfies Record<SidebarVariant, Record<string, string>>;

type SidebarTheme = (typeof THEMES)[SidebarVariant];

function isActivePath(pathname: string, href: string): boolean {
  if (href === '/') return pathname === '/';
  return pathname === href || pathname.startsWith(href + '/');
}

function hasBadge(item: NavItem): boolean {
  return item.badge !== undefined && item.badge > 0;
}

function initialsOf(userName: string): string {
  return userName
    .split(' ')
    .map((n) => n[0])
    .join('')
    .toUpperCase()
    .slice(0, 2);
}

interface NavLinkProps {
  item: NavItem;
  active: boolean;
  collapsed: boolean;
  theme: SidebarTheme;
}

function NavLink({ item, active, collapsed, theme }: NavLinkProps) {
  const showBadge = hasBadge(item);
  return (
    <Link
      href={item.href}
      className={cn(
        'flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-all relative group',
        active ? theme.activeRow : theme.idleRow
      )}
    >
      {active && (
        <div
          className={cn(
            'absolute left-0 top-1/2 -translate-y-1/2 w-[3px] h-5 rounded-r-full',
            theme.activeBorder
          )}
        />
      )}
      <span className={cn('shrink-0', active && theme.activeIcon)}>
        {item.icon}
      </span>
      {!collapsed && (
        <>
          <span className="flex-1 truncate">{item.label}</span>
          {showBadge && (
            <span
              className={cn(
                'text-[10px] font-bold text-white px-1.5 py-0.5 rounded-full min-w-[18px] text-center',
                theme.badge
              )}
            >
              {item.badge}
            </span>
          )}
        </>
      )}
      {collapsed && showBadge && (
        <span
          className={cn(
            'absolute -top-1 -right-1 text-[9px] font-bold text-white w-4 h-4 rounded-full flex items-center justify-center',
            theme.badge
          )}
        >
          {item.badge}
        </span>
      )}
    </Link>
  );
}

interface NavSectionsProps {
  sections: NavSection[];
  collapsed: boolean;
  theme: SidebarTheme;
}

function NavSections({ sections, collapsed, theme }: NavSectionsProps) {
  const pathname = usePathname();
  return (
    <nav className="flex-1 overflow-y-auto py-4 px-2">
      {sections.map((section) => (
        <div key={section.label} className="mb-6">
          {!collapsed && (
            <p
              className={cn(
                'text-[10px] font-semibold uppercase tracking-wider px-3 mb-2',
                theme.sectionLabel
              )}
            >
              {section.label}
            </p>
          )}
          <div className="space-y-0.5">
            {section.items.map((item) => (
              <NavLink
                key={item.href}
                item={item}
                active={isActivePath(pathname, item.href)}
                collapsed={collapsed}
                theme={theme}
              />
            ))}
          </div>
        </div>
      ))}
    </nav>
  );
}

interface UserBlockProps {
  userName: string;
  userRole?: string;
  userAvatar?: ReactNode;
  onSignOut?: () => void;
  collapsed: boolean;
  theme: SidebarTheme;
}

function UserDetails({ userName, userRole, onSignOut, theme }: Omit<UserBlockProps, 'collapsed' | 'userAvatar'>) {
  return (
    <>
      <div className="flex-1 min-w-0">
        <p className={cn('text-sm font-medium truncate', theme.userName)}>
          {userName}
        </p>
        {userRole && (
          <p className={cn('text-xs truncate capitalize', theme.userRole)}>
            {userRole.replace(/_/g, ' ')}
          </p>
        )}
      </div>
      {onSignOut && (
        <button onClick={onSignOut} className={cn('p-1.5 rounded-lg transition-colors', theme.signOut)}>
          <LogOut className="h-4 w-4" />
        </button>
      )}
    </>
  );
}

function UserBlock({ userAvatar, collapsed, ...details }: UserBlockProps) {
  return (
    <div className="flex items-center gap-3">
      {userAvatar ?? (
        <div
          className={cn(
            'h-9 w-9 rounded-full flex items-center justify-center text-xs font-bold shrink-0',
            details.theme.avatar
          )}
        >
          {initialsOf(details.userName)}
        </div>
      )}
      {!collapsed && <UserDetails {...details} />}
    </div>
  );
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
  const theme = THEMES[variant];

  return (
    <aside
      className={cn(
        'fixed inset-y-0 left-0 z-30 flex flex-col border-r transition-all duration-300',
        theme.aside,
        collapsed ? 'w-[72px]' : 'w-[260px]',
        className
      )}
    >
      {/* Logo */}
      <div className={cn('flex items-center gap-3 px-4 h-16 border-b shrink-0', theme.divider)}>
        {collapsed ? (logoCollapsed ?? logo) : logo}
        <button
          onClick={() => setCollapsed(!collapsed)}
          className={cn('ml-auto p-1.5 rounded-lg transition-colors', theme.toggle)}
        >
          {collapsed ? <Menu className="h-4 w-4" /> : <X className="h-4 w-4" />}
        </button>
      </div>

      {/* Navigation */}
      <NavSections sections={sections} collapsed={collapsed} theme={theme} />

      {/* Footer / User */}
      <div className={cn('border-t p-3 shrink-0', theme.divider)}>
        {footer}
        {userName && (
          <UserBlock
            userName={userName}
            userRole={userRole}
            userAvatar={userAvatar}
            onSignOut={onSignOut}
            collapsed={collapsed}
            theme={theme}
          />
        )}
      </div>
    </aside>
  );
}
