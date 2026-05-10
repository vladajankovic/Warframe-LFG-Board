import { type ReactNode } from 'react'

interface CategoryCardProps {
  label: string
  description: string
  icon: string
  onClick: () => void
  iconContent?: ReactNode
}

export default function CategoryCard({ label, description, icon, onClick, iconContent }: CategoryCardProps) {
  return (
    <button className="category-card" onClick={onClick} type="button">
      {iconContent ?? (icon && (
        <img
          className="category-card__icon"
          src={icon}
          alt={label}
          onError={(e) => { (e.currentTarget as HTMLImageElement).style.display = 'none' }}
        />
      ))}
      <h2 className="category-card__title">{label}</h2>
      <p className="category-card__desc">{description}</p>
    </button>
  )
}
