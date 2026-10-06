import type { SVGProps } from 'react'

type IconProps = SVGProps<SVGSVGElement>

export function DashboardIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M4.75 5.75C4.75 5.198 5.198 4.75 5.75 4.75H10.25C10.802 4.75 11.25 5.198 11.25 5.75V10.25C11.25 10.802 10.802 11.25 10.25 11.25H5.75C5.198 11.25 4.75 10.802 4.75 10.25V5.75ZM12.75 5.75C12.75 5.198 13.198 4.75 13.75 4.75H18.25C18.802 4.75 19.25 5.198 19.25 5.75V8.25C19.25 8.802 18.802 9.25 18.25 9.25H13.75C13.198 9.25 12.75 8.802 12.75 8.25V5.75ZM4.75 13.75C4.75 13.198 5.198 12.75 5.75 12.75H8.25C8.802 12.75 9.25 13.198 9.25 13.75V18.25C9.25 18.802 8.802 19.25 8.25 19.25H5.75C5.198 19.25 4.75 18.802 4.75 18.25V13.75ZM12.75 11.75C12.75 11.198 13.198 10.75 13.75 10.75H18.25C18.802 10.75 19.25 11.198 19.25 11.75V18.25C19.25 18.802 18.802 19.25 18.25 19.25H13.75C13.198 19.25 12.75 18.802 12.75 18.25V11.75Z"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

export function PeopleIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M8.75 10.25C10.1307 10.25 11.25 9.13071 11.25 7.75C11.25 6.36929 10.1307 5.25 8.75 5.25C7.36929 5.25 6.25 6.36929 6.25 7.75C6.25 9.13071 7.36929 10.25 8.75 10.25ZM15.75 11.75C17.1307 11.75 18.25 10.6307 18.25 9.25C18.25 7.86929 17.1307 6.75 15.75 6.75C14.3693 6.75 13.25 7.86929 13.25 9.25C13.25 10.6307 14.3693 11.75 15.75 11.75ZM4.75 18.25C4.75 15.9028 6.65279 14 9 14H10.5C12.8472 14 14.75 15.9028 14.75 18.25M13.75 18.25C13.75 16.5931 15.0931 15.25 16.75 15.25H17.25C18.9069 15.25 20.25 16.5931 20.25 18.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

export function ShieldIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M12 4.75L18.25 7.25V11.75C18.25 15.7561 15.4144 19.203 12 20.25C8.58563 19.203 5.75 15.7561 5.75 11.75V7.25L12 4.75ZM9.75 12.25L11.25 13.75L14.75 10.25"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
    </svg>
  )
}

export function WalletIcon(props: IconProps) {
  return (
    <svg aria-hidden="true" fill="none" viewBox="0 0 24 24" {...props}>
      <path
        d="M5.75 7.75H18.25C18.8023 7.75 19.25 8.19772 19.25 8.75V16.25C19.25 16.8023 18.8023 17.25 18.25 17.25H5.75C5.19772 17.25 4.75 16.8023 4.75 16.25V8.75C4.75 8.19772 5.19772 7.75 5.75 7.75ZM4.75 9.75H14.25C14.8023 9.75 15.25 10.1977 15.25 10.75V13.25C15.25 13.8023 14.8023 14.25 14.25 14.25H4.75M16.75 12H16.76"
        stroke="currentColor"
        strokeLinecap="round"
        strokeLinejoin="round"
        strokeWidth="1.8"
      />
      <path d="M6.75 7.75V6.75C6.75 5.92157 7.42157 5.25 8.25 5.25H17.25" stroke="currentColor" strokeLinecap="round" strokeWidth="1.8" />
    </svg>
  )
}
