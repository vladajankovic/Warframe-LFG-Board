import { useLayoutEffect, useRef, useState } from 'react'

interface OverflowTooltipProps {
  text?: string | null
}

export function OverflowTooltip({ text: value }: OverflowTooltipProps) {
  const text = value?.trim() || '—'
  const textRef = useRef<HTMLSpanElement>(null)
  const [isTruncated, setIsTruncated] = useState(false)

  useLayoutEffect(() => {
    const element = textRef.current
    if (!element) return

    const updateTruncation = () => {
      setIsTruncated(element.scrollWidth > element.clientWidth)
    }

    updateTruncation()
    const observer = new ResizeObserver(updateTruncation)
    observer.observe(element)
    return () => observer.disconnect()
  }, [text])

  return (
    <span
      ref={textRef}
      className="admin-table__desc-text"
      title={isTruncated ? text : undefined}
    >
      {text}
    </span>
  )
}
