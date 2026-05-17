import { useNavigate } from 'react-router-dom'

const MENU_ITEMS = [
  {
    id: 'relics',
    title: 'Edit Relics',
    description: 'Add, update or remove relics from the database.',
    icon: '◈',
    path: '/admin/relics',
    available: true,
  },
  {
    id: 'resources',
    title: 'Edit Resources',
    description: 'Add, update or remove resources from the database.',
    icon: '⬡',
    path: '/admin/resources',
    available: true,
  },
  {
    id: 'users',
    title: 'Edit Users',
    description: 'Manage user accounts and roles.',
    icon: '◉',
    path: '/admin/users',
    available: false,
  },
  {
    id: 'logs',
    title: 'View Logs',
    description: 'Inspect application activity logs.',
    icon: '≡',
    path: '/admin/logs',
    available: false,
  },
]

export default function AdminPage() {
  const navigate = useNavigate()

  return (
    <div className="admin-menu">
      <div className="admin-menu__header">
        <h1 className="admin-menu__title">Admin Panel</h1>
        <p className="admin-menu__subtitle">Select a section to manage.</p>
      </div>

      <div className="admin-menu__grid">
        {MENU_ITEMS.map(item => (
          <button
            key={item.id}
            className={`admin-menu__card${item.available ? '' : ' admin-menu__card--disabled'}`}
            onClick={() => item.available && navigate(item.path)}
            disabled={!item.available}
            type="button"
          >
            <span className="admin-menu__card-icon">{item.icon}</span>
            <span className="admin-menu__card-title">{item.title}</span>
            <span className="admin-menu__card-desc">{item.description}</span>
            {!item.available && (
              <span className="admin-menu__card-badge">Coming soon</span>
            )}
          </button>
        ))}
      </div>
    </div>
  )
}